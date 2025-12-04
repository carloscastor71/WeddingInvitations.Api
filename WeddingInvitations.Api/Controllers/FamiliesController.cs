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
            [FromQuery] string? filter = null,
[FromQuery] string? sortBy = "default")
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
                else
                {
                    // Búsqueda por nombre o familia
                    query = query.Where(g =>
                        g.Name.ToLower().Contains(filter) ||
                        g.Family.FamilyName.ToLower().Contains(filter));
                }
            }

            // Contar total antes de paginar
            var totalGuests = await query.CountAsync();

            // Aplicar ordenamiento según el parámetro sortBy
            IOrderedQueryable<Guest> orderedQuery;

            switch (sortBy?.ToLower())
            {
                case "table":
                    // Ordenar por mesa: asignados primero por número de mesa, sin asignar al final
                    orderedQuery = query
                        .OrderBy(g => g.TableId.HasValue ? 0 : 1)
                        .ThenBy(g => g.Table != null ? g.Table.TableNumber : int.MaxValue)
                        .ThenBy(g => g.Name);
                    break;

                case "family":
                    // Ordenar por nombre de familia
                    orderedQuery = query
                        .OrderBy(g => g.Family.FamilyName)
                        .ThenBy(g => g.Name);
                    break;

                case "name":
                    // Ordenar por nombre de invitado
                    orderedQuery = query.OrderBy(g => g.Name);
                    break;

                case "default":
                default:
                    // Orden por defecto: SIN mesa primero, luego CON mesa por número
                    orderedQuery = query
                        .OrderBy(g => g.TableId.HasValue ? 1 : 0)  // Sin mesa primero (0), con mesa después (1)
                        .ThenBy(g => g.Table != null ? g.Table.TableNumber : 0)  // Luego por número de mesa
                        .ThenBy(g => g.Name);  // Luego por nombre
                    break;
            }

            // Paginar y proyectar
            var guests = await orderedQuery
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

        // ===== ⭐ NUEVO ENDPOINT: GENERAR PASES =====

        /// <summary>
        /// Genera TODOS los pases de invitado para una familia
        /// Elimina pases anteriores, genera nuevos PDFs, los guarda en BD
        /// y retorna URLs + mensaje para WhatsApp
        /// </summary>
        /// <param name="id">ID de la familia</param>
        /// <param name="pdfService">Servicio de generación de PDFs</param>
        /// <param name="tempFileManager">Gestor de archivos temporales</param>
        /// <returns>URLs de los PDFs y mensaje de WhatsApp</returns>
        [HttpGet("{id}/generate-passes")]
        public async Task<IActionResult> GeneratePasses(
            int id,
            [FromServices] PdfInvitationService pdfService,
            [FromServices] TempFileManager tempFileManager)
        {
            try
            {
                // 1. Verificar que la familia existe
                var family = await _context.Families
                    .Include(f => f.Guests)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (family == null)
                {
                    return NotFound(new { message = "Familia no encontrada" });
                }

                // 2. Eliminar pases anteriores de esta familia (si existen)
                var deletedCount = await tempFileManager.DeleteFamilyPasses(id);
                if (deletedCount > 0)
                {
                    Console.WriteLine($"🗑️  {deletedCount} pase(s) anterior(es) eliminado(s)");
                }

                // 3. Generar todos los pases nuevos (uno por mesa)
                var generatedPasses = await pdfService.GenerateAllPassesForFamily(id);

                if (!generatedPasses.Any())
                {
                    return BadRequest(new { message = "No se pudo generar ningún pase" });
                }

                // 4. Guardar cada PDF en la base de datos y obtener URLs
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var passesInfo = new List<object>();

                foreach (var pass in generatedPasses)
                {
                    // Guardar en BD
                    var fileName = await tempFileManager.SavePdf(
                        pass.PdfData,
                        pass.FamilyId,
                        pass.TableId,
                        family.InvitationCode,
                        pass.FamilyName
                    );

                    // Generar URL pública
                    var publicUrl = tempFileManager.GetPublicUrl(fileName, baseUrl);

                    passesInfo.Add(new
                    {
                        tableNumber = pass.TableNumber,
                        tableName = pass.TableName,
                        guestCount = pass.GuestCount,
                        fileName = fileName,
                        url = publicUrl,
                        sizeKB = pass.SizeInBytes / 1024
                    });
                }

                // 5. Generar mensaje de WhatsApp
                var whatsappMessage = GenerateWhatsAppMessage(family, passesInfo);

                // 6. Generar URL de WhatsApp
                var whatsappUrl = GenerateWhatsAppUrl(family.Phone, whatsappMessage);

                // 7. Retornar respuesta completa
                return Ok(new
                {
                    success = true,
                    familyName = family.CorrectedFamilyName ?? family.FamilyName,
                    passesCount = passesInfo.Count,
                    passes = passesInfo,
                    whatsappMessage = whatsappMessage,
                    whatsappUrl = whatsappUrl
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al generar pases: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al generar pases",
                    error = ex.Message
                });
            }
        }

        // ===== MÉTODOS AUXILIARES =====

        /// <summary>
        /// Genera el mensaje de WhatsApp con todos los links de pases
        /// </summary>
        private string GenerateWhatsAppMessage(Family family, List<object> passes)
        {
            var familyName = !string.IsNullOrWhiteSpace(family.CorrectedFamilyName)
                ? family.CorrectedFamilyName
                : family.FamilyName;

            var message = $"¡Hola {familyName}! 👋\n\n";
            message += "Les compartimos su(s) pase(s) de invitado para nuestra boda del 20 de diciembre.\n\n";

            if (passes.Count == 1)
            {
                // Un solo pase
                dynamic pass = passes[0];
                message += $"📄 Su pase de invitado:\n";
                message += $"{pass.url}\n\n";

                if (pass.tableNumber != null)
                {
                    message += $"Mesa asignada: #{pass.tableNumber} - {pass.tableName}\n";
                    message += $"({pass.guestCount} invitado(s))\n\n";
                }
            }
            else
            {
                // Múltiples pases
                message += "📄 Sus pases de invitado:\n\n";

                foreach (dynamic pass in passes)
                {
                    if (pass.tableNumber != null)
                    {
                        message += $"Mesa #{pass.tableNumber} - {pass.tableName}\n";
                        message += $"({pass.guestCount} invitado(s))\n";
                        message += $"{pass.url}\n\n";
                    }
                    else
                    {
                        message += $"Pase (sin mesa asignada):\n";
                        message += $"{pass.url}\n\n";
                    }
                }
            }

            message += "Por favor compártanlo(s) con sus acompañantes.\n\n";
            message += "¡Los esperamos! 💒\n";
            message += "Karen & Carlos";

            return message;
        }

        /// <summary>
        /// Genera la URL de WhatsApp para abrir directamente con el mensaje
        /// </summary>
        private string GenerateWhatsAppUrl(string phone, string message)
        {
            // Limpiar teléfono (remover espacios, guiones, etc.)
            var cleanPhone = new string(phone.Where(char.IsDigit).ToArray());

            // Asegurar que tenga código de país
            if (!cleanPhone.StartsWith("52") && cleanPhone.Length == 10)
            {
                cleanPhone = "52" + cleanPhone; // México por defecto
            }

            // Encodear mensaje para URL
            var encodedMessage = Uri.EscapeDataString(message);

            return $"https://wa.me/{cleanPhone}?text={encodedMessage}";
        }
    }
}