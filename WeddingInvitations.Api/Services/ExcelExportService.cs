using OfficeOpenXml;
using Microsoft.EntityFrameworkCore;
using WeddingInvitations.Api.Data;
using WeddingInvitations.Api.Models;

namespace WeddingInvitations.Api.Services
{
    public class ExcelExportService
    {
        private readonly WeddingDbContext _context;

        public ExcelExportService(WeddingDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> ExportGuestsToExcel()
        {
            // Obtener INVITADOS confirmados con sus familias y mesas
            var guests = await _context.Guests
                .Include(g => g.Family)
                .Include(g => g.Table)
                .Where(g => g.Family.Status == "confirmed" && g.Family.FormCompleted)
                .OrderBy(g => g.TableId.HasValue ? 0 : 1)  // Sin mesa al final
                .ThenBy(g => g.Table != null ? g.Table.TableNumber : int.MaxValue)  // Por número de mesa
                .ThenBy(g => g.Family.FamilyName)  // Por familia
                .ThenBy(g => g.Name)  // Por nombre
                .ToListAsync();

            // Configurar EPPlus para uso no comercial
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Invitados Confirmados");

            // ===== CREAR HEADERS (SOLO 5 COLUMNAS) =====
            worksheet.Cells[1, 1].Value = "Mesa";
            worksheet.Cells[1, 2].Value = "Familia";
            worksheet.Cells[1, 3].Value = "Nombre Invitado";
            worksheet.Cells[1, 4].Value = "Tipo";
            worksheet.Cells[1, 5].Value = "Restricciones Alimenticias";

            // Estilo para headers
            using (var range = worksheet.Cells[1, 1, 1, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thick);
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            int row = 2;
            int totalGuests = 0;
            int totalAdults = 0;
            int totalChildren = 0;
            int? currentTableId = null;
            var tableStats = new Dictionary<string, (int adults, int children)>();

            // ===== LLENAR DATOS AGRUPADOS POR MESA =====
            foreach (var guest in guests)
            {
                // Si cambiamos de mesa, insertar fila en blanco
                if (currentTableId != guest.TableId && row > 2)
                {
                    row++; // Fila en blanco entre mesas
                }

                currentTableId = guest.TableId;

                // Columna 1: Mesa (solo número)
                var tableDisplay = guest.Table != null
                    ? $"Mesa {guest.Table.TableNumber}"
                    : "Sin Mesa";
                worksheet.Cells[row, 1].Value = tableDisplay;

                // Columna 2: Familia
                var familyName = string.IsNullOrWhiteSpace(guest.Family.CorrectedFamilyName)
                    ? guest.Family.FamilyName
                    : guest.Family.CorrectedFamilyName;
                worksheet.Cells[row, 2].Value = familyName;

                // Columna 3: Nombre del invitado
                worksheet.Cells[row, 3].Value = guest.Name;

                // Columna 4: Tipo
                var guestType = guest.IsChild ? "Niño" : "Adulto";
                worksheet.Cells[row, 4].Value = guestType;

                // Columna 5: Restricciones alimenticias
                worksheet.Cells[row, 5].Value = guest.DietaryRestrictions ?? "";

                // Estadísticas generales
                totalGuests++;
                if (guest.IsChild)
                    totalChildren++;
                else
                    totalAdults++;

                // Estadísticas por mesa
                if (!tableStats.ContainsKey(tableDisplay))
                {
                    tableStats[tableDisplay] = (0, 0);
                }

                var stats = tableStats[tableDisplay];
                if (guest.IsChild)
                    tableStats[tableDisplay] = (stats.adults, stats.children + 1);
                else
                    tableStats[tableDisplay] = (stats.adults + 1, stats.children);

                row++;
            }

            // ===== AGREGAR RESUMEN POR MESA =====
            row += 2;
            worksheet.Cells[row, 1].Value = "RESUMEN POR MESA";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Size = 14;
            using (var range = worksheet.Cells[row, 1, row, 4])
            {
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
            }
            row++;

            worksheet.Cells[row, 1].Value = "Mesa";
            worksheet.Cells[row, 2].Value = "Adultos";
            worksheet.Cells[row, 3].Value = "Niños";
            worksheet.Cells[row, 4].Value = "Total";
            using (var range = worksheet.Cells[row, 1, row, 4])
            {
                range.Style.Font.Bold = true;
            }
            row++;

            foreach (var tableStat in tableStats.OrderBy(t => t.Key))
            {
                worksheet.Cells[row, 1].Value = tableStat.Key;
                worksheet.Cells[row, 2].Value = tableStat.Value.adults;
                worksheet.Cells[row, 3].Value = tableStat.Value.children;
                worksheet.Cells[row, 4].Value = tableStat.Value.adults + tableStat.Value.children;
                row++;
            }

            // ===== AGREGAR RESUMEN GENERAL =====
            row += 2;
            worksheet.Cells[row, 1].Value = "RESUMEN GENERAL";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Size = 14;
            using (var range = worksheet.Cells[row, 1, row, 2])
            {
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
            }
            row++;

            var totalFamilies = guests.Select(g => g.FamilyId).Distinct().Count();

            worksheet.Cells[row, 1].Value = "Total Familias Confirmadas:";
            worksheet.Cells[row, 2].Value = totalFamilies;
            worksheet.Cells[row, 2].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = "Total Invitados:";
            worksheet.Cells[row, 2].Value = totalGuests;
            worksheet.Cells[row, 2].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = "Adultos:";
            worksheet.Cells[row, 2].Value = totalAdults;
            row++;

            worksheet.Cells[row, 1].Value = "Niños:";
            worksheet.Cells[row, 2].Value = totalChildren;

            // Auto ajustar columnas
            worksheet.Cells.AutoFitColumns();

            // Ajustar ancho mínimo de columnas para mejor legibilidad
            worksheet.Column(1).Width = 15; // Mesa
            worksheet.Column(2).Width = 20; // Familia
            worksheet.Column(3).Width = 25; // Nombre Invitado
            worksheet.Column(4).Width = 10; // Tipo
            worksheet.Column(5).Width = 30; // Restricciones

            return package.GetAsByteArray();
        }
    }
}