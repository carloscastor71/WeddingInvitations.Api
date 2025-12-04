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
                    // Lienzo continuo optimizado para móvil (ancho fijo, alto automático)
                    page.ContinuousSize(400, Unit.Point);
                    page.Margin(25);
                    page.PageColor(Colors.White);

                    // TODO centrado en un único flujo
                    page.Content().AlignCenter().Column(column =>
                    {
                        column.Spacing(18);

                        column.Item().Element(c => ComposeHeader(c));
                        column.Item().Element(c => ComposeTitle(c));
                        column.Item().Element(c => ComposeFamilyInfo(c, data));
                        column.Item().Element(c => ComposeEvents(c));
                        column.Item().Element(c => ComposeGuestsList(c, data));
                        column.Item().Element(c => ComposeDressCode(c));
                        column.Item().Element(c => ComposeGiftMessage(c));
                        column.Item().Element(c => ComposeFooter(c));
                    });
                });
            });

            return document.GeneratePdf();
        }

        // ========================================
        // TODAS LAS SECCIONES CENTRADAS
        // ========================================

        private void ComposeHeader(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().AlignCenter().Width(160).Height(3).Background(WeddingEventInfo.PrimaryColorHex);
            });
        }

        private void ComposeTitle(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                // Nombres - TODO centrado
                column.Item().AlignCenter().Text(text =>
                {
                    text.AlignCenter();
                    text.Span($"{WeddingEventInfo.BrideName} ").FontSize(38).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();
                    text.Span("& ").FontSize(30).FontColor(WeddingEventInfo.SecondaryColorHex);
                    text.Span($"{WeddingEventInfo.GroomName}").FontSize(38).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();
                });

                // Separador
                column.Item().AlignCenter().PaddingTop(10).Width(130).Height(1).Background(WeddingEventInfo.SecondaryColorHex);

                // NOS CASAMOS
                column.Item().AlignCenter().PaddingTop(12).Text("NOS CASAMOS")
                    .FontSize(15).FontColor(WeddingEventInfo.PrimaryColorHex);

                // Fecha
                column.Item().AlignCenter().PaddingTop(6).Text(WeddingEventInfo.WeddingDate)
                    .FontSize(13).FontColor(WeddingEventInfo.SecondaryColorHex);
            });
        }

        private void ComposeFamilyInfo(IContainer container, FamilyPassData data)
        {
            container.AlignCenter().Column(column =>
            {
                // Título
                column.Item().AlignCenter().Text("PASE DE INVITADO")
                    .FontSize(12).FontColor(WeddingEventInfo.PrimaryColorHex);

                column.Item().AlignCenter().PaddingTop(10).Width(260).Height(1).Background(Colors.Grey.Lighten2);

                // Nombre de familia
                column.Item().AlignCenter().PaddingTop(12).Text(data.FamilyName)
                    .FontSize(26).FontColor(Colors.Black).Bold();

                // Mesa
                if (data.TableNumber.HasValue)
                {
                    column.Item().AlignCenter().PaddingTop(15).PaddingHorizontal(30).PaddingVertical(15)
                        .Border(3).BorderColor(WeddingEventInfo.SecondaryColorHex)
                        .AlignCenter().Column(inner =>
                        {
                            inner.Item().AlignCenter().Text($"MESA {data.TableNumber}")
                                .FontSize(24).FontColor(WeddingEventInfo.SecondaryColorHex).Bold();

                            if (!string.IsNullOrEmpty(data.TableName))
                            {
                                inner.Item().AlignCenter().PaddingTop(5).Text(data.TableName)
                                    .FontSize(13).FontColor(Colors.Grey.Darken1);
                            }
                        });
                }
                else
                {
                    column.Item().AlignCenter().PaddingTop(15).Text("Mesa: Por asignar")
                        .FontSize(12).FontColor(Colors.Grey.Darken1).Italic();
                }

                // Código
                column.Item().AlignCenter().PaddingTop(15).Text($"Código: {data.InvitationCode}")
                    .FontSize(10).FontColor(Colors.Grey.Medium);
            });
        }

        private void ComposeEvents(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().AlignCenter().Text("ITINERARIO")
                    .FontSize(12).FontColor(WeddingEventInfo.PrimaryColorHex);

                column.Item().AlignCenter().PaddingTop(10).Width(260).Height(1).Background(Colors.Grey.Lighten2);

                foreach (var evt in WeddingEventInfo.Events)
                {
                    column.Item().AlignCenter().PaddingTop(15).Column(eventColumn =>
                    {
                        // Nombre del evento - CENTRADO
                        eventColumn.Item().AlignCenter().Text(evt.Name.ToUpper())
                            .FontSize(12).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();

                        // Hora - CENTRADO
                        eventColumn.Item().AlignCenter().PaddingTop(6).Text(evt.Time)
                            .FontSize(18).FontColor(Colors.Black).Bold();

                        // Venue - CENTRADO
                        eventColumn.Item().AlignCenter().PaddingTop(4).Text(evt.Venue)
                            .FontSize(13).FontColor(Colors.Black);

                        // Dirección - CENTRADO
                        eventColumn.Item().AlignCenter().PaddingTop(3).PaddingHorizontal(20).Text(evt.Address)
                            .FontSize(11).FontColor(Colors.Grey.Darken1);

                        // Link - CENTRADO
                        eventColumn.Item().AlignCenter().PaddingTop(8).Hyperlink(evt.MapUrl)
                            .Text("Ver ubicación")
                            .FontSize(11).FontColor(WeddingEventInfo.SecondaryColorHex).Underline();
                    });

                    // Separador
                    if (evt != WeddingEventInfo.Events.Last())
                    {
                        column.Item().AlignCenter().PaddingTop(15).Width(190).Height(1).Background(Colors.Grey.Lighten3);
                    }
                }
            });
        }

        private void ComposeGuestsList(IContainer container, FamilyPassData data)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().AlignCenter().Text("INVITADOS CONFIRMADOS")
                    .FontSize(12).FontColor(WeddingEventInfo.PrimaryColorHex);

                column.Item().AlignCenter().PaddingTop(10).Width(260).Height(1).Background(Colors.Grey.Lighten2);

                column.Item().AlignCenter().PaddingTop(12).Column(guestColumn =>
                {
                    foreach (var guest in data.Guests)
                    {
                        guestColumn.Item().AlignCenter().PaddingTop(5).Text(text =>
                        {
                            text.AlignCenter();
                            text.Span(guest.Name).FontSize(14).FontColor(Colors.Black);

                            if (guest.IsChild)
                            {
                                text.Span(" (niño/a)").FontSize(11).FontColor(Colors.Grey.Medium).Italic();
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
                column.Item().AlignCenter().Text("CÓDIGO DE VESTIMENTA")
                    .FontSize(12).FontColor(WeddingEventInfo.PrimaryColorHex);

                column.Item().AlignCenter().PaddingTop(10).Text(WeddingEventInfo.DressCode)
                    .FontSize(20).FontColor(WeddingEventInfo.SecondaryColorHex).Bold();

                column.Item().AlignCenter().PaddingTop(8).PaddingHorizontal(30).Text(WeddingEventInfo.DressCodeDescription)
                    .FontSize(11).FontColor(Colors.Grey.Darken1);

                // Restricciones - CENTRADO
                column.Item().AlignCenter().PaddingTop(10).PaddingHorizontal(30).Text(text =>
                {
                    text.AlignCenter();
                    text.Span("Evita: ").FontSize(11).FontColor(Colors.Grey.Darken2).SemiBold();
                    text.Span(string.Join(" • ", WeddingEventInfo.DressCodeRestrictions))
                        .FontSize(11).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private void ComposeGiftMessage(IContainer container)
        {
            container.AlignCenter().PaddingHorizontal(30).Column(column =>
            {
                column.Item().AlignCenter().Text(WeddingEventInfo.GiftMessageTitle)
                    .FontSize(13).FontColor(WeddingEventInfo.PrimaryColorHex).SemiBold();

                column.Item().AlignCenter().PaddingTop(8).Text(WeddingEventInfo.GiftMessage)
                    .FontSize(11).FontColor(Colors.Grey.Darken1).LineHeight(1.5f);
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().AlignCenter().Width(160).Height(1).Background(WeddingEventInfo.SecondaryColorHex);

                column.Item().AlignCenter().PaddingTop(12).Text(WeddingEventInfo.ClosingMessage)
                    .FontSize(14).FontColor(WeddingEventInfo.PrimaryColorHex).Italic();
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