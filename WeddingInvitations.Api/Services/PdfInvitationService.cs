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
    /// <summary>
    /// Servicio para generar PDFs de pases de invitado personalizados
    /// Usa QuestPDF para crear documentos elegantes con la información de la boda
    /// </summary>
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

        /// <summary>
        /// Genera TODOS los pases de una familia (uno por cada mesa donde tienen invitados)
        /// </summary>
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

        /// <summary>
        /// Genera un PDF para un grupo específico de invitados de una mesa
        /// </summary>
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

        /// <summary>
        /// Genera el documento PDF usando QuestPDF
        /// </summary>
        private byte[] GeneratePdfDocument(FamilyPassData data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(30);
                    page.PageColor(Colors.White);

                    page.Content().Column(column =>
                    {
                        // SECCIÓN 1: Header con imagen decorativa
                        column.Item().Element(c => ComposeHeader(c));

                        column.Item().PaddingVertical(10);

                        // SECCIÓN 2: Título "Karen & Carlos"
                        column.Item().Element(c => ComposeTitle(c));

                        column.Item().PaddingVertical(15);

                        // SECCIÓN 3: Información de familia y mesa
                        column.Item().Element(c => ComposeFamilyInfo(c, data));

                        column.Item().PaddingVertical(15);

                        // SECCIÓN 4: Itinerario de eventos
                        column.Item().Element(c => ComposeEvents(c));

                        column.Item().PaddingVertical(15);

                        // SECCIÓN 5: Código de vestimenta
                        column.Item().Element(c => ComposeDressCode(c));

                        column.Item().PaddingVertical(15);

                        // SECCIÓN 6: Lista de invitados
                        column.Item().Element(c => ComposeGuestsList(c, data));

                        column.Item().PaddingVertical(15);

                        // SECCIÓN 7: Mensaje de regalos
                        column.Item().Element(c => ComposeGiftMessage(c));

                        column.Item().PaddingVertical(15);

                        // SECCIÓN 8: Footer
                        column.Item().Element(c => ComposeFooter(c));
                    });
                });
            });

            return document.GeneratePdf();
        }

        // ========================================
        // SECCIONES DEL PDF
        // ========================================

        /// <summary>
        /// SECCIÓN 1: Header decorativo con imagen
        /// </summary>
        private void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
               

                // Línea decorativa (siempre se muestra)
                column.Item().PaddingTop(10).LineHorizontal(2).LineColor(WeddingEventInfo.PrimaryColorHex);
            });
        }
        /// <summary>
        /// SECCIÓN 2: Título con nombres de los novios
        /// </summary>
        private void ComposeTitle(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().AlignCenter().Text(text =>
                {
                    text.Span($"{WeddingEventInfo.BrideName} ").FontSize(36).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();
                    text.Span("& ").FontSize(28).FontColor(WeddingEventInfo.SecondaryColorHex);
                    text.Span($"{WeddingEventInfo.GroomName}").FontSize(36).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();
                });

                column.Item().PaddingTop(5).AlignCenter().LineHorizontal(1).LineColor(WeddingEventInfo.SecondaryColorHex);

                column.Item().PaddingTop(10).AlignCenter().Text("NOS CASAMOS")
                    .FontSize(18).FontColor(WeddingEventInfo.PrimaryColorHex).LetterSpacing(2);

                column.Item().PaddingTop(5).AlignCenter().Text(WeddingEventInfo.WeddingDate)
                    .FontSize(14).FontColor(WeddingEventInfo.SecondaryColorHex).SemiBold();
            });
        }

        /// <summary>
        /// SECCIÓN 3: Información de familia y mesa
        /// </summary>
        private void ComposeFamilyInfo(IContainer container, FamilyPassData data)
        {
            container.Padding(15).Background(Colors.Grey.Lighten4).Column(column =>
            {
                column.Item().AlignCenter().Text("PASE DE INVITADO")
                    .FontSize(16).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();

                column.Item().PaddingTop(10).PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                column.Item().AlignCenter().PaddingTop(10).Text($"Familia: {data.FamilyName}")
                    .FontSize(14).FontColor(Colors.Black);

                column.Item().AlignCenter().PaddingTop(5).Text($"Invitados confirmados: {data.ConfirmedGuests}")
                    .FontSize(12).FontColor(Colors.Grey.Darken2);

                // Mesa asignada
                if (data.TableNumber.HasValue)
                {
                    column.Item().PaddingTop(10).Padding(10).Background(WeddingEventInfo.SecondaryColorHex).Column(inner =>
                    {
                        inner.Item().AlignCenter().Text($"Mesa Asignada: #{data.TableNumber}")
                            .FontSize(16).FontColor(Colors.White).Bold();

                        if (!string.IsNullOrEmpty(data.TableName))
                        {
                            inner.Item().PaddingTop(3).AlignCenter().Text(data.TableName)
                                .FontSize(12).FontColor(Colors.White);
                        }
                    });
                }
                else
                {
                    column.Item().PaddingTop(10).Padding(10).Background(Colors.Grey.Lighten2).AlignCenter()
                        .Text("Mesa: Pendiente de asignación")
                        .FontSize(12).FontColor(Colors.Grey.Darken2).Italic();
                }

                column.Item().PaddingTop(10).AlignCenter().Text($"Código de invitación: {data.InvitationCode}")
                    .FontSize(10).FontColor(Colors.Grey.Medium);
            });
        }

        /// <summary>
        /// SECCIÓN 4: Itinerario de eventos
        /// </summary>
        private void ComposeEvents(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().AlignCenter().Text("ITINERARIO DEL DÍA")
                    .FontSize(16).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();

                column.Item().PaddingTop(5).AlignCenter().LineHorizontal(1).LineColor(WeddingEventInfo.SecondaryColorHex);

                foreach (var evt in WeddingEventInfo.Events)
                {
                    column.Item().PaddingTop(15).Padding(10).Border(1).BorderColor(Colors.Grey.Lighten2).Column(eventColumn =>
                    {
                        // Icono y nombre del evento
                        eventColumn.Item().AlignCenter().Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.Span($"{evt.Icon} ").FontSize(18);
                                text.Span(evt.Name).FontSize(14).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();
                            });
                        });

                        // Hora
                        eventColumn.Item().AlignCenter().PaddingTop(5).Text($"Hora: {evt.Time}")
                            .FontSize(12).FontColor(Colors.Black);

                        // Lugar
                        eventColumn.Item().AlignCenter().PaddingTop(3).Text(evt.Venue)
                            .FontSize(12).FontColor(Colors.Black).SemiBold();

                        // Dirección
                        eventColumn.Item().AlignCenter().PaddingTop(2).Text(evt.Address)
                            .FontSize(10).FontColor(Colors.Grey.Darken1);

                        // Nota adicional si existe
                        if (!string.IsNullOrEmpty(evt.Note))
                        {
                            eventColumn.Item().PaddingTop(3).Text(evt.Note)
                                .FontSize(9).FontColor(Colors.Grey.Medium).Italic();
                        }

                        // Botón de Google Maps
                        eventColumn.Item().PaddingTop(8).AlignCenter().Hyperlink(evt.MapUrl).Padding(8)
                            .Background(WeddingEventInfo.SecondaryColorHex)
                            .AlignCenter().Text("📍 Ver en Google Maps")
                            .FontSize(11).FontColor(Colors.White).SemiBold();
                    });
                }
            });
        }

        /// <summary>
        /// SECCIÓN 5: Código de vestimenta
        /// </summary>
        private void ComposeDressCode(IContainer container)
        {
            container.Padding(10).Background(Colors.Grey.Lighten4).Column(column =>
            {
                column.Item().AlignCenter().Text("CÓDIGO DE VESTIMENTA")
                    .FontSize(14).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();

                column.Item().PaddingTop(5).AlignCenter().Text(WeddingEventInfo.DressCode)
                    .FontSize(18).FontColor(WeddingEventInfo.SecondaryColorHex).Bold();

                column.Item().PaddingTop(5).AlignCenter().Text(WeddingEventInfo.DressCodeDescription)
                    .FontSize(10).FontColor(Colors.Grey.Darken2);

                column.Item().AlignCenter().PaddingTop(10).Text("Por favor evita:")
                    .FontSize(10).FontColor(Colors.Black).SemiBold();

                foreach (var restriction in WeddingEventInfo.DressCodeRestrictions)
                {
                    column.Item().AlignCenter().PaddingTop(3).PaddingLeft(10).Text($"• {restriction}")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                }
            });
        }

        /// <summary>
        /// SECCIÓN 6: Lista de invitados confirmados
        /// </summary>
        private void ComposeGuestsList(IContainer container, FamilyPassData data)
        {
            container.Padding(10).Border(1).BorderColor(Colors.Grey.Lighten2).Column(column =>
            {
                column.Item().AlignCenter().Text("INVITADOS CONFIRMADOS")
                    .FontSize(14).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();

                column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                foreach (var guest in data.Guests)
                {
                    column.Item().AlignCenter().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("✓ ").FontSize(12).FontColor(WeddingEventInfo.SecondaryColorHex);
                            text.Span(guest.Name).FontSize(11).FontColor(Colors.Black);

                            if (guest.IsChild)
                            {
                                text.Span(" (niño/a)").FontSize(9).FontColor(Colors.Grey.Medium).Italic();
                            }
                        });
                    });
                }
            });
        }

        /// <summary>
        /// SECCIÓN 7: Mensaje de mesa de regalos
        /// </summary>
        private void ComposeGiftMessage(IContainer container)
        {
            container.Padding(10).Background(Colors.Grey.Lighten4).Column(column =>
            {
                column.Item().AlignCenter().Text(WeddingEventInfo.GiftMessageTitle)
                    .FontSize(14).FontColor(WeddingEventInfo.PrimaryColorHex).Bold();

                column.Item().AlignCenter().PaddingTop(8).Text(WeddingEventInfo.GiftMessage)
                    .FontSize(10).FontColor(Colors.Grey.Darken2).LineHeight(1.5f);
            });
        }

        /// <summary>
        /// SECCIÓN 8: Footer con mensaje de cierre
        /// </summary>
        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(WeddingEventInfo.SecondaryColorHex);

                column.Item().PaddingTop(10).AlignCenter().Text(WeddingEventInfo.ClosingMessage)
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