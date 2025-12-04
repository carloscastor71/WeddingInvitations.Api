using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WeddingInvitations.Api.Services;

namespace WeddingInvitations.Api.Controllers
{
    /// <summary>
    /// Controller para servir PDFs de pases de invitado
    /// Endpoint público: /api/passes/{fileName}
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PassesController : ControllerBase
    {
        private readonly TempFileManager _tempFileManager;

        public PassesController(TempFileManager tempFileManager)
        {
            _tempFileManager = tempFileManager;
        }

        /// <summary>
        /// Sirve un PDF de pase de invitado por su nombre de archivo
        /// Este es el endpoint que se abre cuando alguien hace click en el link de WhatsApp
        /// </summary>
        /// <param name="fileName">Nombre del archivo PDF (ej: Garcia_Mesa5_20241128.pdf)</param>
        /// <returns>Archivo PDF o error 404 si no existe/expiró</returns>
        [HttpGet("{fileName}")]
        public async Task<IActionResult> GetPass(string fileName)
        {
            try
            {
                // Validar que el fileName termine en .pdf
                if (!fileName.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "El archivo debe ser un PDF" });
                }

                // Buscar el PDF en la base de datos
                var pass = await _tempFileManager.GetPdfByFileName(fileName);

                if (pass == null)
                {
                    return NotFound(new
                    {
                        message = "Pase no encontrado o expirado",
                        hint = "Los pases expiran después de 24 horas. Por favor solicita un nuevo pase."
                    });
                }

                // Retornar el archivo PDF
                return File(
                    pass.PdfData,                    // Bytes del PDF
                    "application/pdf",                // Content-Type
                    fileName,                         // Nombre del archivo para descarga
                    enableRangeProcessing: true       // Permite descarga parcial/resumible
                );
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"❌ Error al servir PDF {fileName}: {ex.Message}");
                return StatusCode(500, new
                {
                    message = "Error al obtener el pase",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Endpoint opcional para verificar si un pase existe sin descargarlo
        /// Útil para debugging
        /// </summary>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Info del pase o 404</returns>
        [HttpHead("{fileName}")]
        [HttpGet("{fileName}/info")]
        public async Task<IActionResult> GetPassInfo(string fileName)
        {
            try
            {
                var pass = await _tempFileManager.GetPdfByFileName(fileName);

                if (pass == null)
                {
                    return NotFound(new { message = "Pase no encontrado o expirado" });
                }

                return Ok(new
                {
                    fileName = pass.FileName,
                    familyId = pass.FamilyId,
                    tableId = pass.TableId,
                    sizeKB = pass.SizeInBytes / 1024,
                    createdAt = pass.CreatedAt,
                    expiresAt = pass.ExpiresAt,
                    isExpired = pass.ExpiresAt < System.DateTime.UtcNow
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error", error = ex.Message });
            }
        }
    }
}