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

        private bool FamilyExists(int id)
        {
            return _context.Families.Any(e => e.Id == id);
        }
    }
}