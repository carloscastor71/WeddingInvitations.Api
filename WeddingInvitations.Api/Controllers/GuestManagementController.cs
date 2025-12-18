using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingInvitations.Api.Data;
using WeddingInvitations.Api.Models;

namespace WeddingInvitations.Api.Controllers
{
    [ApiController]
    [Route("api/guest-management")]
    public class GuestManagementController : ControllerBase
    {
        private readonly WeddingDbContext _context;

        public GuestManagementController(WeddingDbContext context)
        {
            _context = context;
        }

        // POST: api/guest-management/guests
        // Agregar nuevo invitado (a familia existente o creando nueva familia)
        [HttpPost("guests")]
        public async Task<ActionResult> AddGuest([FromBody] AddGuestRequest request)
        {
            // Validar datos del invitado
            if (string.IsNullOrWhiteSpace(request.Guest.Name))
            {
                return BadRequest(new { message = "El nombre del invitado es obligatorio" });
            }

            Family family;

            // CASO 1: Agregar a familia existente
            if (request.FamilyId.HasValue)
            {
                family = await _context.Families
                    .Include(f => f.Guests)
                    .FirstOrDefaultAsync(f => f.Id == request.FamilyId.Value);

                if (family == null)
                {
                    return NotFound(new { message = "Familia no encontrada" });
                }

                // Verificar si no excede MaxGuests (opcional, podrías auto-incrementar)
                if (family.Guests.Count >= family.MaxGuests)
                {
                    // Auto-incrementar MaxGuests
                    family.MaxGuests = family.Guests.Count + 1;
                }
            }
            // CASO 2: Crear nueva familia
            else
            {
                if (request.NewFamily == null)
                {
                    return BadRequest(new { message = "Debe proporcionar datos de la familia o seleccionar una existente" });
                }

                // Validar datos de nueva familia
                if (string.IsNullOrWhiteSpace(request.NewFamily.FamilyName) ||
                    string.IsNullOrWhiteSpace(request.NewFamily.ContactPerson) ||
                    string.IsNullOrWhiteSpace(request.NewFamily.Phone))
                {
                    return BadRequest(new { message = "Datos obligatorios de la familia faltantes" });
                }

                // Crear nueva familia
                family = new Family
                {
                    FamilyName = request.NewFamily.FamilyName.Trim(),
                    ContactPerson = request.NewFamily.ContactPerson.Trim(),
                    Phone = request.NewFamily.Phone.Trim(),
                    Email = string.IsNullOrWhiteSpace(request.NewFamily.Email)
                        ? null
                        : request.NewFamily.Email.Trim(),
                    Country = string.IsNullOrWhiteSpace(request.NewFamily.Country)
                        ? "MX"
                        : request.NewFamily.Country.Trim(),
                    MaxGuests = 1, // Inicialmente 1 (el invitado que se está agregando)
                    InvitationCode = Family.GenerateInvitationCode(),
                    Status = "draft",
                    ConfirmedGuests = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Families.Add(family);
                await _context.SaveChangesAsync(); // Guardar para obtener el ID
            }

            // Crear el invitado
            var guest = new Guest
            {
                FamilyId = family.Id,
                Name = request.Guest.Name.Trim(),
                IsChild = request.Guest.IsChild,
                Age = request.Guest.IsChild ? "child" : "adult",
                DietaryRestrictions = string.IsNullOrWhiteSpace(request.Guest.DietaryRestrictions)
                    ? null
                    : request.Guest.DietaryRestrictions.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Guest.Notes)
                    ? null
                    : request.Guest.Notes.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Guests.Add(guest);

            // Actualizar contadores de la familia
            family.ConfirmedGuests++;
            family.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Recargar la familia con todos sus invitados para la respuesta
            await _context.Entry(family).Collection(f => f.Guests).LoadAsync();

            return Ok(new
            {
                message = "Invitado agregado exitosamente",
                family = new
                {
                    family.Id,
                    family.FamilyName,
                    family.CorrectedFamilyName,
                    family.ContactPerson,
                    family.MaxGuests,
                    family.ConfirmedGuests,
                    family.Status,
                    family.InvitationCode
                },
                guest = new
                {
                    guest.Id,
                    guest.Name,
                    guest.IsChild,
                    guest.DietaryRestrictions,
                    guest.Notes
                }
            });
        }

        // PUT: api/guest-management/guests/{id}
        // Modificar datos de un invitado existente
        [HttpPut("guests/{id}")]
        public async Task<ActionResult> UpdateGuest(int id, [FromBody] UpdateGuestRequest request)
        {
            // Validar datos
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "El nombre del invitado es obligatorio" });
            }

            var guest = await _context.Guests
                .Include(g => g.Family)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (guest == null)
            {
                return NotFound(new { message = "Invitado no encontrado" });
            }

            // Actualizar campos
            guest.Name = request.Name.Trim();
            guest.IsChild = request.IsChild;
            guest.Age = request.IsChild ? "child" : "adult";
            guest.DietaryRestrictions = string.IsNullOrWhiteSpace(request.DietaryRestrictions)
                ? null
                : request.DietaryRestrictions.Trim();
            guest.Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? null
                : request.Notes.Trim();

            // Actualizar timestamp de la familia
            guest.Family.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Invitado actualizado exitosamente",
                guest = new
                {
                    guest.Id,
                    guest.Name,
                    guest.IsChild,
                    guest.DietaryRestrictions,
                    guest.Notes,
                    FamilyId = guest.Family.Id,
                    FamilyName = guest.Family.CorrectedFamilyName ?? guest.Family.FamilyName
                }
            });
        }

        // GET: api/guest-management/families/dropdown
        // Obtener lista simplificada de familias para el dropdown
        [HttpGet("families/dropdown")]
        public async Task<ActionResult<IEnumerable<FamilyDropdownItem>>> GetFamiliesForDropdown()
        {
            var families = await _context.Families
                .Include(f => f.Guests)
                .OrderBy(f => f.CorrectedFamilyName ?? f.FamilyName)
                .Select(f => new FamilyDropdownItem
                {
                    Id = f.Id,
                    DisplayName = f.CorrectedFamilyName ?? f.FamilyName,
                    CurrentGuests = f.Guests.Count,
                    MaxGuests = f.MaxGuests,
                    Status = f.Status
                })
                .ToListAsync();

            return Ok(families);
        }

        // GET: api/guest-management/guests/search
        // Buscar invitados (con filtros opcionales)
        [HttpGet("guests/search")]
        public async Task<ActionResult<IEnumerable<GuestSearchResult>>> SearchGuests(
            [FromQuery] int? familyId,
            [FromQuery] string? search)
        {
            var query = _context.Guests
                .Include(g => g.Family)
                .AsQueryable();

            // Filtrar por familia si se especifica
            if (familyId.HasValue)
            {
                query = query.Where(g => g.FamilyId == familyId.Value);
            }

            // Filtrar por nombre si se especifica
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(g => g.Name.ToLower().Contains(searchLower));
            }

            var guests = await query
                .OrderBy(g => g.Family.CorrectedFamilyName ?? g.Family.FamilyName)
                .ThenBy(g => g.Name)
                .Select(g => new GuestSearchResult
                {
                    Id = g.Id,
                    Name = g.Name,
                    IsChild = g.IsChild,
                    DietaryRestrictions = g.DietaryRestrictions,
                    Notes = g.Notes,
                    FamilyId = g.Family.Id,
                    FamilyName = g.Family.CorrectedFamilyName ?? g.Family.FamilyName
                })
                .ToListAsync();

            return Ok(guests);
        }

        // DELETE: api/guest-management/guests/{id}
        // Eliminar un invitado (opcional - útil para correcciones)
        [HttpDelete("guests/{id}")]
        public async Task<ActionResult> DeleteGuest(int id)
        {
            var guest = await _context.Guests
                .Include(g => g.Family)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (guest == null)
            {
                return NotFound(new { message = "Invitado no encontrado" });
            }

            var family = guest.Family;
            var guestName = guest.Name;

            // Eliminar invitado
            _context.Guests.Remove(guest);

            // Actualizar contadores de la familia
            family.ConfirmedGuests = Math.Max(0, family.ConfirmedGuests - 1);
            family.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Invitado '{guestName}' eliminado exitosamente",
                familyId = family.Id,
                newGuestCount = family.ConfirmedGuests
            });
        }
    }

    // ===== DTOs (Data Transfer Objects) =====

    public class AddGuestRequest
    {
        public int? FamilyId { get; set; }
        public NewFamilyData? NewFamily { get; set; }
        public GuestData Guest { get; set; } = new GuestData();
    }

    public class NewFamilyData
    {
        public string FamilyName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Country { get; set; }
    }

    public class GuestData
    {
        public string Name { get; set; } = string.Empty;
        public bool IsChild { get; set; }
        public string? DietaryRestrictions { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateGuestRequest
    {
        public string Name { get; set; } = string.Empty;
        public bool IsChild { get; set; }
        public string? DietaryRestrictions { get; set; }
        public string? Notes { get; set; }
    }

    public class FamilyDropdownItem
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int CurrentGuests { get; set; }
        public int MaxGuests { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class GuestSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsChild { get; set; }
        public string? DietaryRestrictions { get; set; }
        public string? Notes { get; set; }
        public int FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
    }
}