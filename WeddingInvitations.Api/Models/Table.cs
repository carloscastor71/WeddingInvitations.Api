using System.ComponentModel.DataAnnotations;

namespace WeddingInvitations.Api.Models
{
    public class Table
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TableNumber { get; set; } // 1-14

        [Required]
        [StringLength(50)]
        public string TableName { get; set; } = string.Empty; // "Mesa 1", "Mesa 2"... "Mesa de Honor"

        public int MaxCapacity { get; set; } = 10;

        public int CurrentOccupancy { get; set; } = 0;

        // Navegación
        public List<Guest> Guests { get; set; } = new List<Guest>();
    }
}