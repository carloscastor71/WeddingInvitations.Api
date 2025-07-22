using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingInvitations.Api.Models
{
    public class Guest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FamilyId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string Age { get; set; } = "adult"; // adult, child, baby
       
        public bool IsChild { get; set; } = false; // Flag específico para niños
        [StringLength(500)]
        public string? Notes { get; set; } // Notas específicas por persona

        [StringLength(500)]
        public string? DietaryRestrictions { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación hacia la familia
        [ForeignKey("FamilyId")]
        public Family Family { get; set; } = null!;
    }
}