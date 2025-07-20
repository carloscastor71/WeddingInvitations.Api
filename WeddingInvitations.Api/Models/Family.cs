using System.ComponentModel.DataAnnotations;
using System.Linq; 

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

        [StringLength(8)]
        public string InvitationCode { get; set; } = string.Empty;

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


        // Información de eventos - 20 de Diciembre 2025
        public DateTime ReligiousDateTime { get; set; } = DateTime.SpecifyKind(new DateTime(2025, 12, 20, 23, 30, 0), DateTimeKind.Utc);
        public string ReligiousVenue { get; set; } = "Parroquia De San Agustín";
        public string ReligiousAddress { get; set; } = "Paseo Viento Sur 350, 27258 Torreón";

        public DateTime CivilDateTime { get; set; } = DateTime.SpecifyKind(new DateTime(2025, 12, 21, 2, 0, 0), DateTimeKind.Utc);
        public string CivilVenue { get; set; } = "Salon MONET";
        public string CivilAddress { get; set; } = "Cll Lisboa 101 Granjas de San Isidro, 27100 Torreón, Coahuila";

        public DateTime ReceptionDateTime { get; set; } = DateTime.SpecifyKind(new DateTime(2025, 12, 21, 2, 30, 0), DateTimeKind.Utc);
        public string ReceptionVenue { get; set; } = "Salon MONET";
        public string ReceptionAddress { get; set; } = "Cll Lisboa 101 Granjas de San Isidro, 27100 Torreón, Coahuila";

        // Estados del flujo de confirmación
        public bool InvitationViewed { get; set; } = false;
        public DateTime? ViewedDate { get; set; }
        public bool FormCompleted { get; set; } = false;
        public DateTime? FormCompletedDate { get; set; }

        // Recordatorios
        public DateTime? LastReminderSent { get; set; }
        public int ReminderCount { get; set; } = 0;
        public DateTime ResponseDeadline { get; set; } = DateTime.SpecifyKind(new DateTime(2025, 10, 31), DateTimeKind.Utc);

        // Método para generar código único
        public static string GenerateInvitationCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

}