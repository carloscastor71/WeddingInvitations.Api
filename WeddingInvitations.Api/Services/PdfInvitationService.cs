using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WeddingInvitations.Api.Data;
using WeddingInvitations.Api.Models;

namespace WeddingInvitations.Api.Services
{
    public class PdfInvitationService
    {
        private readonly WeddingDbContext _context;
        private readonly ILogger<PdfInvitationService> _logger;
        private readonly IWebHostEnvironment _environment;

        public PdfInvitationService(
            WeddingDbContext context,
            ILogger<PdfInvitationService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        public async Task<List<GeneratedPassInfo>> GenerateAllPassesForFamily(int familyId)
        {
            try
            {
                _logger.LogInformation($"📄 Generando pases para familia ID: {familyId}");

                var family = await _context.Families
                    .Include(f => f.Guests)
                        .ThenInclude(g => g.Table)
                    .FirstOrDefaultAsync(f => f.Id == familyId);

                if (family == null)
                {
                    throw new InvalidOperationException($"Familia con ID {familyId} no encontrada");
                }

                var guestsGroupedByTable = family.Guests
                    .Where(g => g.TableId.HasValue)
                    .GroupBy(g => new
                    {
                        TableId = g.TableId!.Value,
                        TableNumber = g.Table!.TableNumber,
                        TableName = g.Table!.TableName
                    })
                    .ToList();

                if (!guestsGroupedByTable.Any())
                {
                    _logger.LogWarning($"⚠️  Familia {family.FamilyName} sin mesa asignada");
                    var passWithoutTable = await GeneratePdfForTable(family, null, null, null, family.Guests.ToList());
                    return new List<GeneratedPassInfo> { passWithoutTable };
                }

                var generatedPasses = new List<GeneratedPassInfo>();

                foreach (var tableGroup in guestsGroupedByTable)
                {
                    var passInfo = await GeneratePdfForTable(
                        family,
                        tableGroup.Key.TableId,
                        tableGroup.Key.TableNumber,
                        tableGroup.Key.TableName,
                        tableGroup.ToList()
                    );
                    generatedPasses.Add(passInfo);
                }

                _logger.LogInformation($"✅ {generatedPasses.Count} pase(s) generado(s) para {family.FamilyName}");
                return generatedPasses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error al generar pases para familia {familyId}");
                throw;
            }
        }

        private async Task<GeneratedPassInfo> GeneratePdfForTable(
            Family family,
            int? tableId,
            int? tableNumber,
            string? tableName,
            List<Guest> guestsInTable)
        {
            var passData = new FamilyPassData
            {
                FamilyId = family.Id,
                FamilyName = string.IsNullOrWhiteSpace(family.CorrectedFamilyName)
                    ? family.FamilyName
                    : family.CorrectedFamilyName,
                ContactPerson = family.ContactPerson,
                InvitationCode = family.InvitationCode,
                ConfirmedGuests = guestsInTable.Count,
                TableId = tableId,
                TableNumber = tableNumber,
                TableName = tableName,
                Guests = guestsInTable.Select(g => new GuestInfo
                {
                    Name = g.Name,
                    IsChild = g.IsChild
                }).ToList()
            };

            var pdfBytes = GeneratePdfDocument(passData);

            return new GeneratedPassInfo
            {
                FamilyId = family.Id,
                FamilyName = passData.FamilyName,
                TableId = tableId,
                TableNumber = tableNumber,
                TableName = tableName,
                GuestCount = guestsInTable.Count,
                PdfData = pdfBytes,
                SizeInBytes = pdfBytes.Length
            };
        }

        private byte[] GeneratePdfDocument(FamilyPassData data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.PageColor(Colors.White);

                    page.Content().Column(column =>
                    {
                        // Espaciado superior
                        column.Item().Height(20);

                        // Header minimalista
                        column.Item().Element(c => ComposeHeader(c));

                        column.Item().Height(20);

                        // Título
                        column.Item().Element(c => ComposeTitle(c));

                        column.Item().Height(25);

                        // Info de familia
                        column.Item().Element(c => ComposeFamilyInfo(c, data));

                        column.Item().Height(25);

                        // Eventos
                        column.Item().Element(c => ComposeEvents(c));

                        column.Item().Height(20);

                        // Lista de invitados
                        column.Item().Element(c => ComposeGuestsList(c, data));

                        column.Item().Height(20);

                        // Código de vestimenta
                        column.Item().Element(c => ComposeDressCode(c));

                        column.Item().Height(20);

                        // Mensaje de regalos
                        column.Item().Element(c => ComposeGiftMessage(c));

                        column.Item().Height(25);

                        // Footer
                        column.Item().Element(c => ComposeFooter(c));
                    });
                });
            });

            return document.GeneratePdf();
        }

        // ========================================
        // SECCIONES DEL PDF - DISEÑO MINIMALISTA
        // ========================================

        private void ComposeHeader(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                // Línea decorativa superior
                column.Item().Width(200).Height(2).Background(WeddingEventInfo.PrimaryColorHex);
            });
        }

        private void ComposeTitle(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                // Nombres grandes y elegantes
                column.Item().Text(text =>
                {
                    text.AlignCenter();
                    text.Span($"{WeddingEventInfo.BrideName} ").FontSize(40).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();
                    text.Span("&").FontSize(32).FontColor(WeddingEventInfo.SecondaryColorHex).Light();
                    text.Span($" {WeddingEventInfo.GroomName}").FontSize(40).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();
                });

                column.Item().PaddingTop(8).Width(150).Height(1).Background(WeddingEventInfo.SecondaryColorHex);

                column.Item().PaddingTop(12).Text("NOS CASAMOS")
                    .FontSize(16).FontColor(WeddingEventInfo.PrimaryColorHex).LetterSpacing(3).Light();

                column.Item().PaddingTop(6).Text(WeddingEventInfo.WeddingDate)
                    .FontSize(13).FontColor(WeddingEventInfo.SecondaryColorHex);
            });
        }

        private void ComposeFamilyInfo(IContainer container, FamilyPassData data)
        {
            container.AlignCenter().Column(column =>
            {
                // Título de sección
                column.Item().Text("PASE DE INVITADO")
                    .FontSize(12).FontColor(WeddingEventInfo.PrimaryColorHex).LetterSpacing(2).Light();

                column.Item().PaddingTop(12).Width(300).Height(1).Background(Colors.Grey.Lighten2);

                // Nombre de familia destacado
                column.Item().PaddingTop(12).Text(data.FamilyName)
                    .FontSize(22).FontColor(Colors.Black).Bold();

                // Info de mesa
                if (data.TableNumber.HasValue)
                {
                    column.Item().PaddingTop(15).PaddingHorizontal(20).PaddingVertical(12)
                        .Border(2).BorderColor(WeddingEventInfo.SecondaryColorHex)
                        .Column(inner =>
                        {
                            inner.Item().Text($"MESA {data.TableNumber}")
                                .FontSize(20).FontColor(WeddingEventInfo.SecondaryColorHex).Bold();

                            if (!string.IsNullOrEmpty(data.TableName))
                            {
                                inner.Item().PaddingTop(3).Text(data.TableName)
                                    .FontSize(11).FontColor(Colors.Grey.Darken1);
                            }
                        });
                }
                else
                {
                    column.Item().PaddingTop(15).Text("Mesa: Por asignar")
                        .FontSize(11).FontColor(Colors.Grey.Darken1).Italic();
                }

                // Código pequeño al final
                column.Item().PaddingTop(15).Text($"Código: {data.InvitationCode}")
                    .FontSize(9).FontColor(Colors.Grey.Medium);
            });
        }

        private void ComposeEvents(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().Text("ITINERARIO")
                    .FontSize(12).FontColor(WeddingEventInfo.PrimaryColorHex).LetterSpacing(2).Light();

                column.Item().PaddingTop(12).Width(300).Height(1).Background(Colors.Grey.Lighten2);

                foreach (var evt in WeddingEventInfo.Events)
                {
                    column.Item().PaddingTop(15).Column(eventColumn =>
                    {
                        // Nombre del evento
                        eventColumn.Item().Text(evt.Name.ToUpper())
                            .FontSize(12).FontColor(WeddingEventInfo.PrimaryColorHex).LetterSpacing(1).SemiBold();

                        // Hora
                        eventColumn.Item().PaddingTop(4).Text(evt.Time)
                            .FontSize(14).FontColor(Colors.Black).Bold();

                        // Venue
                        eventColumn.Item().PaddingTop(3).Text(evt.Venue)
                            .FontSize(11).FontColor(Colors.Black);

                        // Dirección
                        eventColumn.Item().PaddingTop(2).Text(evt.Address)
                            .FontSize(9).FontColor(Colors.Grey.Darken1);

                        // Link de Maps (más discreto)
                        eventColumn.Item().PaddingTop(6).Hyperlink(evt.MapUrl)
                            .Text("Ver ubicacion")
                            .FontSize(9).FontColor(WeddingEventInfo.SecondaryColorHex).Underline();
                    });

                    // Separador entre eventos (excepto el último)
                    if (evt != WeddingEventInfo.Events.Last())
                    {
                        column.Item().PaddingTop(12).Width(200).Height(1).Background(Colors.Grey.Lighten3);
                    }
                }
            });
        }

        private void ComposeGuestsList(IContainer container, FamilyPassData data)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().Text("INVITADOS CONFIRMADOS")
                    .FontSize(12).FontColor(WeddingEventInfo.PrimaryColorHex).LetterSpacing(2).Light();

                column.Item().PaddingTop(12).Width(300).Height(1).Background(Colors.Grey.Lighten2);

                column.Item().PaddingTop(12).Column(guestColumn =>
                {
                    foreach (var guest in data.Guests)
                    {
                        guestColumn.Item().PaddingTop(6).Text(text =>
                        {
                            text.Span(guest.Name).FontSize(11).FontColor(Colors.Black);

                            if (guest.IsChild)
                            {
                                text.Span(" (niño/a)").FontSize(9).FontColor(Colors.Grey.Medium).Italic();
                            }
                        });
                    }
                });
            });
        }

        private void ComposeDressCode(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().Text("CODIGO DE VESTIMENTA")
                    .FontSize(12).FontColor(WeddingEventInfo.PrimaryColorHex).LetterSpacing(2).Light();

                column.Item().PaddingTop(8).Text(WeddingEventInfo.DressCode)
                    .FontSize(16).FontColor(WeddingEventInfo.SecondaryColorHex).Bold();

                column.Item().PaddingTop(6).Text(WeddingEventInfo.DressCodeDescription)
                    .FontSize(9).FontColor(Colors.Grey.Darken1);

                // Restricciones en línea
                column.Item().PaddingTop(8).Text(text =>
                {
                    text.Span("Evita: ").FontSize(9).FontColor(Colors.Grey.Darken2).SemiBold();
                    text.Span(string.Join(" • ", WeddingEventInfo.DressCodeRestrictions))
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private void ComposeGiftMessage(IContainer container)
        {
            container.AlignCenter().PaddingHorizontal(40).Column(column =>
            {
                column.Item().Text(WeddingEventInfo.GiftMessageTitle)
                    .FontSize(11).FontColor(WeddingEventInfo.PrimaryColorHex).SemiBold();

                column.Item().PaddingTop(6).Text(WeddingEventInfo.GiftMessage)
                    .FontSize(9).FontColor(Colors.Grey.Darken1).LineHeight(1.4f);
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().Width(200).Height(1).Background(WeddingEventInfo.SecondaryColorHex);

                column.Item().PaddingTop(12).Text(WeddingEventInfo.ClosingMessage)
                    .FontSize(12).FontColor(WeddingEventInfo.PrimaryColorHex).Italic();
            });
        }
    }

    // ========================================
    // CLASES DE DATOS
    // ========================================

    public class FamilyPassData
    {
        public int FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string InvitationCode { get; set; } = string.Empty;
        public int ConfirmedGuests { get; set; }
        public int? TableId { get; set; }
        public int? TableNumber { get; set; }
        public string? TableName { get; set; }
        public List<GuestInfo> Guests { get; set; } = new();
    }

    public class GuestInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool IsChild { get; set; }
    }

    public class GeneratedPassInfo
    {
        public int FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public int? TableId { get; set; }
        public int? TableNumber { get; set; }
        public string? TableName { get; set; }
        public int GuestCount { get; set; }
        public byte[] PdfData { get; set; } = Array.Empty<byte>();
        public long SizeInBytes { get; set; }
    }
}