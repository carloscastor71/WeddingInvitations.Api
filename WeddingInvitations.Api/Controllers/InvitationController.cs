using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingInvitations.Api.Data;
using WeddingInvitations.Api.Models;

namespace WeddingInvitations.Api.Controllers
{
    [ApiController]
    [Route("api/invitation")]
    public class InvitationController : ControllerBase
    {
        private readonly WeddingDbContext _context;

        public InvitationController(WeddingDbContext context)
        {
            _context = context;
        }

        // GET: api/invitation/{code} - Obtener invitación por código
        [HttpGet("{code}")]
        public async Task<ActionResult<object>> GetInvitation(string code)
        {
            var family = await _context.Families
                .Include(f => f.Guests)
                .FirstOrDefaultAsync(f => f.InvitationCode == code);

            if (family == null)
            {
                return NotFound(new { message = "Invitación no encontrada" });
            }

            // Marcar como vista si es la primera vez
            if (!family.InvitationViewed)
            {
                family.InvitationViewed = true;
                family.ViewedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                familyName = family.FamilyName,
                contactPerson = family.ContactPerson,
                maxGuests = family.MaxGuests,
                confirmedGuests = family.ConfirmedGuests,
                hasResponded = family.Responded,
                isAttending = family.Attending,
                formCompleted = family.FormCompleted,
                responseDeadline = family.ResponseDeadline,
                guests = family.Guests,
                correctedFamilyName = family.CorrectedFamilyName,

                // Información de eventos
                events = new[]
                {
                    new {
                        name = "Ceremonia Religiosa",
                        dateTime = family.ReligiousDateTime,
                        venue = family.ReligiousVenue,
                        address = family.ReligiousAddress,
                        requiresConfirmation = false
                    },
                    new {
                        name = "Ceremonia Civil",
                        dateTime = family.CivilDateTime,
                        venue = family.CivilVenue,
                        address = family.CivilAddress,
                        requiresConfirmation = false
                    },
                    new {
                        name = "Recepción",
                        dateTime = family.ReceptionDateTime,
                        venue = family.ReceptionVenue,
                        address = family.ReceptionAddress,
                        requiresConfirmation = true
                    }
                }
            });
        }

        // POST: api/invitation/{code}/respond - Confirmar asistencia
        [HttpPost("{code}/respond")]
        public async Task<ActionResult> RespondToInvitation(string code, [FromBody] RespondRequest request)
        {
            var family = await _context.Families.FirstOrDefaultAsync(f => f.InvitationCode == code);
            if (family == null)
            {
                return NotFound(new { message = "Invitación no encontrada" });
            }

            // ✅ DEADLINE ELIMINADO
            // La fecha límite se muestra en mensajes solo para crear urgencia,
            // pero técnicamente el sistema permite respuestas en cualquier momento

            // Actualizar respuesta
            family.Responded = true;
            family.ResponseDate = DateTime.UtcNow;
            family.Attending = request.Attending;
            family.UpdatedAt = DateTime.UtcNow;

            if (request.Attending)
            {
                family.Status = "pending"; // Pendiente de completar formulario
            }
            else
            {
                family.Status = "declined";
                family.FormCompleted = true; // No necesita formulario si no asiste
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Respuesta guardada exitosamente" });
        }

        // AGREGAR este método al InvitationController.cs existente (después del método RespondToInvitation)

        // POST: api/invitation/{code}/complete-form - Completar formulario de invitados
        [HttpPost("{code}/complete-form")]
        public async Task<ActionResult> CompleteGuestForm(string code, [FromBody] CompleteFormRequest request)
        {
            var family = await _context.Families
                .Include(f => f.Guests)
                .FirstOrDefaultAsync(f => f.InvitationCode == code);

            if (family == null)
            {
                return NotFound(new { message = "Invitación no encontrada" });
            }

            if (!family.Attending.HasValue || !family.Attending.Value)
            {
                return BadRequest(new { message = "Debe confirmar asistencia primero" });
            }

            if (family.FormCompleted)
            {
                return BadRequest(new { message = "El formulario ya fue completado" });
            }

            // Validar número de invitados
            if (request.Guests.Count > family.MaxGuests)
            {
                return BadRequest(new { message = $"Excede el límite de {family.MaxGuests} invitados" });
            }

            if (request.Guests.Count == 0)
            {
                return BadRequest(new { message = "Debe registrar al menos un invitado" });
            }

            if (!string.IsNullOrWhiteSpace(request.CorrectedFamilyName))
            {
                family.CorrectedFamilyName = request.CorrectedFamilyName.Trim();
            }
            // Limpiar invitados existentes
            _context.Guests.RemoveRange(family.Guests);

            // Agregar nuevos invitados
            foreach (var guestData in request.Guests)
            {
                var guest = new Guest
                {
                    FamilyId = family.Id,
                    Name = guestData.Name.Trim(),
                    Age = guestData.IsChild ? "child" : "adult",
                    IsChild = guestData.IsChild,
                    DietaryRestrictions = string.IsNullOrWhiteSpace(guestData.DietaryRestrictions)
                        ? null : guestData.DietaryRestrictions.Trim(),
                    Notes = string.IsNullOrWhiteSpace(guestData.Notes)
                        ? null : guestData.Notes.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Guests.Add(guest);
            }

            // Actualizar familia
            family.FormCompleted = true;
            family.FormCompletedDate = DateTime.UtcNow;
            family.ConfirmedGuests = request.Guests.Count;
            family.Status = "confirmed";
            family.UpdatedAt = DateTime.UtcNow;

            // Mensaje especial de la familia (opcional)
            if (!string.IsNullOrWhiteSpace(request.FamilyMessage))
            {
                family.SpecialMessage = request.FamilyMessage.Trim();
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Formulario completado exitosamente",
                confirmedGuests = family.ConfirmedGuests,
                correctedFamilyName = family.CorrectedFamilyName
            });
        }

        // AGREGAR estas clases al final del archivo (después de RespondRequest)
        public class CompleteFormRequest
        {
            public List<GuestRequest> Guests { get; set; } = new List<GuestRequest>();
            public string? FamilyMessage { get; set; }
            public string? CorrectedFamilyName { get; set; }
        }

        public class GuestRequest
        {
            public string Name { get; set; } = string.Empty;
            public bool IsChild { get; set; } = false;
            public string? DietaryRestrictions { get; set; }
            public string? Notes { get; set; }
        }
        // Clase para el request de respuesta
        public class RespondRequest
        {
            public bool Attending { get; set; }
        }
    }
}