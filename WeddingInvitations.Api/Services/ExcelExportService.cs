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
            // Obtener familias confirmadas con sus invitados
            var families = await _context.Families
                .Include(f => f.Guests)
                .Where(f => f.Status == "confirmed" && f.FormCompleted)
                .OrderBy(f => f.FamilyName)
                .ToListAsync();

            // Configurar EPPlus para uso no comercial
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Invitados Confirmados");

            // Crear headers
            worksheet.Cells[1, 1].Value = "Familia";
            worksheet.Cells[1, 2].Value = "Contacto Principal";
            worksheet.Cells[1, 3].Value = "Teléfono";
            worksheet.Cells[1, 4].Value = "Email";
            worksheet.Cells[1, 5].Value = "Nombre Invitado";
            worksheet.Cells[1, 6].Value = "Tipo";
            worksheet.Cells[1, 7].Value = "Restricciones Alimenticias";
            worksheet.Cells[1, 8].Value = "Notas Especiales";
            worksheet.Cells[1, 9].Value = "Fecha Confirmación";

            // Estilo para headers
            using (var range = worksheet.Cells[1, 1, 1, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thick);
            }

            int row = 2;
            int totalGuests = 0;
            int totalAdults = 0;
            int totalChildren = 0;

            // Llenar datos de cada invitado
            foreach (var family in families)
            {
                foreach (var guest in family.Guests)
                {
                    worksheet.Cells[row, 1].Value = string.IsNullOrWhiteSpace(family.CorrectedFamilyName) ? family.FamilyName : family.CorrectedFamilyName;
                    worksheet.Cells[row, 2].Value = family.ContactPerson;
                    worksheet.Cells[row, 3].Value = family.Phone;
                    worksheet.Cells[row, 4].Value = family.Email ?? "";
                    worksheet.Cells[row, 5].Value = guest.Name;
                    worksheet.Cells[row, 6].Value = guest.IsChild ? "Niño" : "Adulto";
                    worksheet.Cells[row, 7].Value = guest.DietaryRestrictions ?? "";
                    worksheet.Cells[row, 8].Value = guest.Notes ?? "";
                    worksheet.Cells[row, 9].Value = family.FormCompletedDate?.ToString("dd/MM/yyyy HH:mm") ?? "";

                    totalGuests++;
                    if (guest.IsChild) totalChildren++; else totalAdults++;
                    row++;
                }
            }

            // Agregar resumen al final
            row += 2;
            worksheet.Cells[row, 1].Value = "RESUMEN FINAL";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Size = 14;
            row++;

            worksheet.Cells[row, 1].Value = "Total Familias Confirmadas:";
            worksheet.Cells[row, 2].Value = families.Count;
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

            return package.GetAsByteArray();
        }
    }
}