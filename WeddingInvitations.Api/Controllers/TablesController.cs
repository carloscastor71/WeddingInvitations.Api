using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingInvitations.Api.Data;
using WeddingInvitations.Api.Models;

namespace WeddingInvitations.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TablesController : ControllerBase
    {
        private readonly WeddingDbContext _context;

        public TablesController(WeddingDbContext context)
        {
            _context = context;
        }

        // GET: api/tables/summary
        // Obtiene todas las mesas con su información de ocupación
        [HttpGet("summary")]
        public async Task<IActionResult> GetTablesSummary()
        {
            var tables = await _context.Tables
                .Select(t => new
                {
                    t.Id,
                    t.TableNumber,
                    t.TableName,
                    t.CurrentOccupancy,
                    t.MaxCapacity,
                    AvailableSeats = t.MaxCapacity - t.CurrentOccupancy,
                    PercentageOccupied = t.CurrentOccupancy > 0
                        ? (t.CurrentOccupancy * 100) / t.MaxCapacity
                        : 0,
                    IsFull = t.CurrentOccupancy >= t.MaxCapacity,
                    IsHonorTable = t.TableName == "Mesa de Honor"
                })
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            return Ok(tables);
        }

        // GET: api/tables/available
        // Obtiene solo las mesas que tienen espacio disponible
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableTables()
        {
            var tables = await _context.Tables
                .Where(t => t.CurrentOccupancy < t.MaxCapacity)
                .Select(t => new
                {
                    t.Id,
                    t.TableName,
                    t.TableNumber,
                    AvailableSeats = t.MaxCapacity - t.CurrentOccupancy,
                    Display = $"{t.TableName} ({t.MaxCapacity - t.CurrentOccupancy} disponibles)"
                })
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            return Ok(tables);
        }

        // GET: api/tables/{id}/guests
        // Obtiene los invitados asignados a una mesa específica
        [HttpGet("{id}/guests")]
        public async Task<IActionResult> GetTableGuests(int id)
        {
            var table = await _context.Tables
                .Include(t => t.Guests)
                    .ThenInclude(g => g.Family)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (table == null)
                return NotFound("Mesa no encontrada");

            var result = new
            {
                table.Id,
                table.TableName,
                table.CurrentOccupancy,
                table.MaxCapacity,
                Guests = table.Guests.Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.IsChild,
                    FamilyName = g.Family.CorrectedFamilyName ?? g.Family.FamilyName,
                    g.Notes
                }).OrderBy(g => g.FamilyName).ThenBy(g => g.Name)
            };

            return Ok(result);
        }

        // PUT: api/guests/{guestId}/assign-table
        // Asigna o cambia la mesa de un invitado
        [HttpPut("/api/guests/{guestId}/assign-table")]
        public async Task<IActionResult> AssignTable(int guestId, [FromBody] AssignTableRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var guest = await _context.Guests
                    .Include(g => g.Table)
                    .FirstOrDefaultAsync(g => g.Id == guestId);

                if (guest == null)
                    return NotFound("Invitado no encontrado");

                // Si tenía mesa asignada, decrementar contador de la mesa anterior
                if (guest.TableId.HasValue && guest.Table != null)
                {
                    guest.Table.CurrentOccupancy--;
                    if (guest.Table.CurrentOccupancy < 0)
                        guest.Table.CurrentOccupancy = 0;
                }

                // Si se está asignando a una nueva mesa
                if (request.TableId.HasValue)
                {
                    var newTable = await _context.Tables.FindAsync(request.TableId.Value);
                    if (newTable == null)
                        return NotFound("Mesa no encontrada");

                    // Validar que hay espacio
                    if (newTable.CurrentOccupancy >= newTable.MaxCapacity)
                        return BadRequest(new { error = "La mesa está llena" });

                    newTable.CurrentOccupancy++;
                    guest.TableId = request.TableId;
                }
                else
                {
                    // Quitar asignación de mesa
                    guest.TableId = null;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Retornar información actualizada
                return Ok(new
                {
                    success = true,
                    guestId = guest.Id,
                    tableId = guest.TableId,
                    message = guest.TableId.HasValue
                        ? "Mesa asignada correctamente"
                        : "Asignación de mesa removida"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Error al asignar mesa", details = ex.Message });
            }
        }

        // GET: api/tables/stats
        // Obtiene estadísticas generales del sistema de mesas
        [HttpGet("stats")]
        public async Task<IActionResult> GetTablesStats()
        {
            var totalGuests = await _context.Guests
                .CountAsync(g => g.Family.Status == "confirmed");

            var assignedGuests = await _context.Guests
                .CountAsync(g => g.Family.Status == "confirmed" && g.TableId.HasValue);

            var totalCapacity = await _context.Tables
                .SumAsync(t => t.MaxCapacity);

            var totalOccupied = await _context.Tables
                .SumAsync(t => t.CurrentOccupancy);

            return Ok(new
            {
                TotalGuests = totalGuests,
                AssignedGuests = assignedGuests,
                UnassignedGuests = totalGuests - assignedGuests,
                TotalCapacity = totalCapacity,
                TotalOccupied = totalOccupied,
                AvailableSeats = totalCapacity - totalOccupied,
                PercentageAssigned = totalGuests > 0
                    ? (assignedGuests * 100) / totalGuests
                    : 0
            });
        }
    }

    // DTO para la asignación de mesa
    public class AssignTableRequest
    {
        public int? TableId { get; set; }
    }
}