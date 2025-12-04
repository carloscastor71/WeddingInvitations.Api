using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WeddingInvitations.Api.Data;
using WeddingInvitations.Api.Models;

namespace WeddingInvitations.Api.Services
{
    /// <summary>
    /// Servicio para gestionar PDFs temporales almacenados en PostgreSQL
    /// Los PDFs se guardan en la tabla TempPdfPasses y se eliminan automáticamente después de 24 horas
    /// Diseñado para Railway (sin filesystem persistente)
    /// </summary>
    public class TempFileManager
    {
        private readonly WeddingDbContext _context;
        private readonly ILogger<TempFileManager> _logger;

        public TempFileManager(WeddingDbContext context, ILogger<TempFileManager> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Guarda un PDF en la base de datos
        /// </summary>
        public async Task<string> SavePdf(
            byte[] pdfBytes,
            int familyId,
            int? tableId,
            string invitationCode,
            string familyName)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var tablePart = tableId.HasValue ? $"_Mesa{tableId}" : "_SinMesa";
                var fileName = $"{SanitizeFileName(familyName)}{tablePart}_{timestamp}.pdf";

                var tempPass = new TempPdfPass
                {
                    FamilyId = familyId,
                    TableId = tableId,
                    InvitationCode = invitationCode,
                    FileName = fileName,
                    PdfData = pdfBytes,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    SizeInBytes = pdfBytes.Length
                };

                _context.TempPdfPasses.Add(tempPass);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ PDF guardado: {fileName} ({pdfBytes.Length / 1024} KB)");
                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error al guardar PDF");
                throw;
            }
        }

        /// <summary>
        /// Elimina todos los PDFs de una familia
        /// </summary>
        public async Task<int> DeleteFamilyPasses(int familyId)
        {
            var passes = await _context.TempPdfPasses
                .Where(p => p.FamilyId == familyId)
                .ToListAsync();

            if (passes.Any())
            {
                _context.TempPdfPasses.RemoveRange(passes);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"🗑️  {passes.Count} PDF(s) eliminados");
            }

            return passes.Count;
        }

        /// <summary>
        /// Obtiene un PDF por nombre de archivo
        /// </summary>
        public async Task<TempPdfPass?> GetPdfByFileName(string fileName)
        {
            return await _context.TempPdfPasses
                .FirstOrDefaultAsync(p =>
                    p.FileName == fileName &&
                    p.ExpiresAt > DateTime.UtcNow);
        }

        /// <summary>
        /// Limpia PDFs expirados
        /// </summary>
        public async Task<int> CleanupExpiredPasses()
        {
            var expired = await _context.TempPdfPasses
                .Where(p => p.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expired.Any())
            {
                _context.TempPdfPasses.RemoveRange(expired);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"🧹 {expired.Count} PDF(s) expirados eliminados");
            }

            return expired.Count;
        }

        /// <summary>
        /// Genera URL pública del PDF
        /// </summary>
        public string GetPublicUrl(string fileName, string baseUrl)
        {
            baseUrl = baseUrl.TrimEnd('/');
            return $"{baseUrl}/api/passes/{fileName}";
        }

        /// <summary>
        /// Limpia caracteres especiales del nombre
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            return fileName
                .Replace(" ", "_")
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n")
                .Replace("Á", "A").Replace("É", "E").Replace("Í", "I")
                .Replace("Ó", "O").Replace("Ú", "U").Replace("Ñ", "N");
        }
    }
}