using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingInvitations.Api.Models
{
    /// <summary>
    /// Almacena PDFs de pases de invitado temporalmente en PostgreSQL
    /// Los PDFs expiran después de 24 horas y se eliminan automáticamente
    /// </summary>
    public class TempPdfPass
    {
        /// <summary>
        /// Identificador único del pase temporal
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID de la familia (nullable porque puede no estar asignada aún)
        /// Relación con la tabla Families
        /// </summary>
        public int? FamilyId { get; set; }

        /// <summary>
        /// ID de la mesa asignada (nullable porque puede no tener mesa aún)
        /// Se usa para agrupar invitados de la misma familia en diferentes mesas
        /// Ejemplo: Familia García puede tener pases para Mesa 5 y Mesa 8
        /// </summary>
        public int? TableId { get; set; }

        /// <summary>
        /// Código de invitación de la familia (ej: "ABC123")
        /// Permite búsquedas rápidas sin hacer JOIN
        /// Útil para regenerar todos los pases de una familia
        /// </summary>
        [Required]
        [StringLength(50)]
        public string InvitationCode { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del archivo PDF (ej: "Garcia_Mesa5_20241128_153045.pdf")
        /// Debe ser único para poder servir el archivo por URL
        /// Formato: {FamilyName}_{MesaX}_{Timestamp}.pdf
        /// </summary>
        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Datos binarios del PDF completo
        /// Se almacena como bytea en PostgreSQL
        /// Típicamente entre 50KB - 500KB por PDF
        /// </summary>
        [Required]
        public byte[] PdfData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Fecha y hora de creación del PDF (UTC)
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha y hora de expiración (UTC)
        /// Por defecto: CreatedAt + 24 horas
        /// Después de esta fecha, el PDF se elimina automáticamente
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Tamaño del PDF en bytes (para estadísticas y monitoreo)
        /// Útil para saber cuánto espacio ocupan los PDFs en la BD
        /// </summary>
        public long SizeInBytes { get; set; }

        // ===== RELACIONES DE NAVEGACIÓN =====

        /// <summary>
        /// Navegación hacia la familia que posee este pase
        /// Nullable porque FamilyId es nullable
        /// </summary>
        [ForeignKey("FamilyId")]
        public Family? Family { get; set; }

        /// <summary>
        /// Navegación hacia la mesa asignada en el pase
        /// Nullable porque TableId es nullable
        /// </summary>
        [ForeignKey("TableId")]
        public Table? Table { get; set; }
    }
}