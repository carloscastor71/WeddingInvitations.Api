using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingInvitations.Api.Data;
using WeddingInvitations.Api.Models;
using WeddingInvitations.Api.Services;

namespace WeddingInvitations.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FamiliesController : ControllerBase
    {
        private readonly WeddingDbContext _context;

        public FamiliesController(WeddingDbContext context)
        {
            _context = context;
        }

        // GET: api/families
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Family>>> GetFamilies()
        {
            return await _context.Families.Include(f => f.Guests).ToListAsync();
        }

        // GET: api/families/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Family>> GetFamily(int id)
        {
            var family = await _context.Families
                .Include(f => f.Guests)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (family == null)
            {
                return NotFound();
            }

            return family;
        }

        // POST: api/families
        [HttpPost]
        public async Task<ActionResult<Family>> CreateFamily(Family family)
        {
            //  Validaciones básicas
            if (string.IsNullOrWhiteSpace(family.FamilyName) ||
                string.IsNullOrWhiteSpace(family.ContactPerson) ||
                string.IsNullOrWhiteSpace(family.Phone))
            {
                return BadRequest(new { message = "Datos obligatorios faltantes" });
            }

            //  País por defecto si no viene
            if (string.IsNullOrWhiteSpace(family.Country))
            {
                family.Country = "MX";
            }
            family.InvitationCode = Family.GenerateInvitationCode();
            family.CreatedAt = DateTime.UtcNow;
            family.UpdatedAt = DateTime.UtcNow;

            _context.Families.Add(family);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFamily), new { id = family.Id }, family);
        }
        // PUT: api/families/5/mark-sent
        [HttpPut("{id}/mark-sent")]
        public async Task<IActionResult> MarkAsSent(int id)
        {
            var family = await _context.Families.FindAsync(id);
            if (family == null)
            {
                return NotFound();
            }

            family.InvitationSent = true;
            family.SentDate = DateTime.UtcNow;
            family.Status = "pending";
            family.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(family);
        }

        // PUT: api/families/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFamily(int id, Family family)
        {
            if (id != family.Id)
            {
                return BadRequest();
            }

            family.UpdatedAt = DateTime.UtcNow;
            _context.Entry(family).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FamilyExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/families/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFamily(int id)
        {
            var family = await _context.Families
                .Include(f => f.Guests)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (family == null)
            {
                return NotFound(new { message = "Familia no encontrada" });
            }

            var guestCount = family.Guests.Count;
            var familyName = family.FamilyName;

            try
            {
                _context.Families.Remove(family);
                await _context.SaveChangesAsync();

                // ✅ IMPORTANTE: Siempre devolver JSON válido
                return Ok(new
                {
                    message = $"Familia '{familyName}' eliminada exitosamente",
                    deletedGuests = guestCount,
                    success = true
                });
            }
            catch (Exception ex)
            {
                // ✅ También en error, devolver JSON válido
                return StatusCode(500, new
                {
                    message = "Error al eliminar la familia",
                    error = ex.Message,
                    success = false
                });
            }
        }
        // GET: api/families/export-excel
        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportGuestsToExcel([FromServices] ExcelExportService excelService)
        {
            try
            {
                var excelData = await excelService.ExportGuestsToExcel();
                var fileName = $"Invitados_Boda_Karen_Carlos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                return File(excelData,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating Excel: {ex.Message}");
                return BadRequest(new { message = "Error al generar Excel", error = ex.Message });
            }
        }
        // GET: api/families/guests-for-assignment
        // Obtiene todos los invitados confirmados para asignación de mesas
        [HttpGet("guests-for-assignment")]
        public async Task<IActionResult> GetGuestsForAssignment(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? filter = null)
        {
            var query = _context.Guests
                .Include(g => g.Family)
                .Include(g => g.Table)
                .Where(g => g.Family.Status == "confirmed");

            // Aplicar filtro si existe
            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.ToLower();
                if (filter == "unassigned")
                {
                    query = query.Where(g => g.TableId == null);
                }
                else if (filter == "assigned")
                {
                    query = query.Where(g => g.TableId != null);
                }
            }

            // Contar total antes de paginar
            var totalGuests = await query.CountAsync();

            // Ordenar: primero sin mesa, luego por familia y nombre
            var guests = await query
                .OrderBy(g => g.TableId.HasValue ? 1 : 0)
                .ThenBy(g => g.Family.FamilyName)
                .ThenBy(g => g.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.IsChild,
                    g.Notes,
                    FamilyId = g.Family.Id,
                    FamilyName = g.Family.CorrectedFamilyName ?? g.Family.FamilyName,
                    TableId = g.TableId,
                    TableName = g.Table != null ? g.Table.TableName : null,
                    Country = g.Family.Country
                })
                .ToListAsync();

            return Ok(new
            {
                Data = guests,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalGuests,
                    TotalPages = (int)Math.Ceiling(totalGuests / (double)pageSize)
                }
            });
        }

        private bool FamilyExists(int id)
        {
            return _context.Families.Any(e => e.Id == id);
        }
    }
}