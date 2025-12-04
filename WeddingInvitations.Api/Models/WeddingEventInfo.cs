using System;
using System.Collections.Generic;

namespace WeddingInvitations.Api.Models
{
    /// <summary>
    /// Información estática de la boda - Hardcoded
    /// </summary>
    public static class WeddingEventInfo
    {
        // ===== INFORMACIÓN DE NOVIOS =====
        public static string BrideName => "Karen";
        public static string GroomName => "Carlos";
        public static string WeddingDate => "Sábado, 20 de Diciembre 2025";
        public static string WeddingDateShort => "20 de Diciembre 2025";

        // ===== EVENTOS =====
        public static List<WeddingEvent> Events => new()
        {
            new WeddingEvent
            {
                Name = "Ceremonia Religiosa",
                Icon = "⛪",
                Time = "5:30 PM",
                Venue = "Parroquia De San Agustín",
                Address = "Paseo Viento Sur 350, 27258 Torreón",
                MapUrl = GenerateGoogleMapsUrl("Paseo Viento Sur 350, 27258 Torreón")
            },
            new WeddingEvent
            {
                Name = "Ceremonia Civil",
                Icon = "💍",
                Time = "8:00 PM",
                Venue = "Salon MONARCA",
                Address = "Cll Lisboa 101 Granjas de San Isidro, 27100 Torreón, Coahuila",
                MapUrl = GenerateGoogleMapsUrl("Cll Lisboa 101 Granjas de San Isidro, 27100 Torreón, Coahuila")
            },
            new WeddingEvent
            {
                Name = "Recepción",
                Icon = "🎉",
                Time = "8:30 PM",
                Venue = "Salon MONARCA",
                Address = "Cll Lisboa 101 Granjas de San Isidro, 27100 Torreón, Coahuila",
                MapUrl = GenerateGoogleMapsUrl("Cll Lisboa 101 Granjas de San Isidro, 27100 Torreón, Coahuila"),
                Note = "(Misma ubicación que la ceremonia civil)"
            }
        };

        // ===== CÓDIGO DE VESTIMENTA =====
        public static string DressCode => "FORMAL";
        public static string DressCodeDescription => "Te sugerimos vestir elegante para esta ocasión especial";
        public static List<string> DressCodeRestrictions => new()
        {
            "Blanco o marfil (reservado para la novia)",
            "Rojo intenso (muy llamativo)",
            "Colores neón (muy brillantes)"
        };

        // ===== MESA DE REGALOS =====
        public static string GiftMessageTitle => "Un Detalle de Amor";
        public static string GiftMessage =>
            "Tu presencia es el regalo más valioso que podemos recibir.\n" +
            "Si deseas tener un detalle adicional, habrá un buzón en la recepción. 💌";

        // ===== MENSAJE FINAL =====
        public static string ClosingMessage => "Con todo nuestro amor, Karen & Carlos";

        // ===== FECHA LÍMITE RSVP =====
        public static string RsvpDeadline => "05 de Diciembre 2025";

        // ===== COLORES (para QuestPDF) =====
        // Estos se usarán en el PDF, QuestPDF usa strings hex
        public static string PrimaryColorHex => "#4c0013";      // Borgoña/vino oscuro
        public static string SecondaryColorHex => "#586e26";    // Verde sage
        public static string LightColorHex => "#fffff0";        // Crema/marfil
        public static string TextColorHex => "#2d2d2d";         // Gris oscuro para texto

        // ===== HELPER METHODS =====
        private static string GenerateGoogleMapsUrl(string address)
        {
            var encoded = Uri.EscapeDataString(address);
            return $"https://www.google.com/maps/search/?api=1&query={encoded}";
        }
    }

    /// <summary>
    /// Modelo para evento individual de la boda
    /// </summary>
    public class WeddingEvent
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Venue { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string MapUrl { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}