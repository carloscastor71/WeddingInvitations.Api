using System.ComponentModel.DataAnnotations;

namespace WeddingInvitations.Api.Models
{
    public class Family
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string FamilyName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ContactPerson { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }

        [Required]
        public int MaxGuests { get; set; } = 2;

        public Guid InvitationCode { get; set; } = Guid.NewGuid();

        public bool InvitationSent { get; set; } = false;
        public DateTime? SentDate { get; set; }
        public bool Responded { get; set; } = false;
        public DateTime? ResponseDate { get; set; }
        public bool? Attending { get; set; } // null = no ha respondido, true = sí asiste, false = no asiste
        public int ConfirmedGuests { get; set; } = 0;

        [StringLength(20)]
        public string Status { get; set; } = "draft"; // draft, pending, confirmed, declined

        [StringLength(1000)]
        public string? DietaryRestrictions { get; set; }

        [StringLength(1000)]
        public string? SpecialMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación a invitados
        public List<Guest> Guests { get; set; } = new List<Guest>();
    }
}