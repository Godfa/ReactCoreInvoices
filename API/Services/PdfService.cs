using System;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace API.Services
{
    public class PdfService : IPdfService
    {
        private readonly DataContext _context;
        private readonly ILogger<PdfService> _logger;

        public PdfService(DataContext context, ILogger<PdfService> logger)
        {
            _context = context;
            _logger = logger;

            // Set QuestPDF license (Community license is free for open source)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Participants)
                        .ThenInclude(p => p.AppUser)
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.Organizer)
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.Payers)
                            .ThenInclude(p => p.AppUser)
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.LineItems)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    throw new Exception($"Invoice with id {invoiceId} not found");

                _logger.LogInformation("Generating full invoice PDF for invoice {InvoiceId}", invoiceId);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                        page.Header()
                            .Column(column =>
                            {
                                column.Item().Text($"Lasku: {invoice.Title}")
                                    .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text($"{invoice.Description}")
                                    .FontSize(12).FontColor(Colors.Grey.Darken1);
                                column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            });

                        page.Content()
                            .Column(column =>
                            {
                                // Participants section
                                column.Item().PaddingTop(10).Text("Osallistujat").FontSize(14).Bold();
                                column.Item().PaddingBottom(10).Text(
                                    string.Join(", ", invoice.Participants?.Select(p => p.AppUser?.DisplayName ?? "Unknown") ?? new string[0])
                                ).FontSize(10);

                                // Expense items
                                column.Item().PaddingTop(10).Text("Kuluerittely").FontSize(14).Bold();

                                if (invoice.ExpenseItems != null && invoice.ExpenseItems.Any())
                                {
                                    foreach (var expenseItem in invoice.ExpenseItems)
                                    {
                                        column.Item().PaddingTop(10).Column(expenseColumn =>
                                        {
                                            // Expense item header
                                            expenseColumn.Item().Background(Colors.Grey.Lighten3).Padding(5).Row(row =>
                                            {
                                                row.RelativeItem().Text($"{expenseItem.Name}").FontSize(12).Bold();
                                                row.ConstantItem(100).AlignRight().Text($"{expenseItem.Amount:F2} €").FontSize(12).Bold();
                                            });

                                            // Organizer and payer info
                                            expenseColumn.Item().Padding(5).Row(row =>
                                            {
                                                row.RelativeItem().Text($"Järjestäjä: {expenseItem.Organizer?.DisplayName ?? "Unknown"}").FontSize(9);
                                                row.RelativeItem().Text($"Maksajat ({expenseItem.Payers?.Count ?? 0}): {string.Join(", ", expenseItem.Payers?.Select(p => p.AppUser?.DisplayName ?? "Unknown") ?? new string[0])}")
                                                    .FontSize(9);
                                            });

                                            // Line items
                                            if (expenseItem.LineItems != null && expenseItem.LineItems.Any())
                                            {
                                                expenseColumn.Item().Table(table =>
                                                {
                                                    table.ColumnsDefinition(columns =>
                                                    {
                                                        columns.RelativeColumn(3);
                                                        columns.ConstantColumn(60);
                                                        columns.ConstantColumn(80);
                                                        columns.ConstantColumn(80);
                                                    });

                                                    table.Header(header =>
                                                    {
                                                        header.Cell().Padding(3).Text("Tuote").FontSize(9).Bold();
                                                        header.Cell().Padding(3).AlignRight().Text("Määrä").FontSize(9).Bold();
                                                        header.Cell().Padding(3).AlignRight().Text("À hinta").FontSize(9).Bold();
                                                        header.Cell().Padding(3).AlignRight().Text("Yhteensä").FontSize(9).Bold();
                                                    });

                                                    foreach (var lineItem in expenseItem.LineItems)
                                                    {
                                                        table.Cell().Padding(3).Text(lineItem.Name).FontSize(9);
                                                        table.Cell().Padding(3).AlignRight().Text($"{lineItem.Quantity}").FontSize(9);
                                                        table.Cell().Padding(3).AlignRight().Text($"{lineItem.UnitPrice:F2} €").FontSize(9);
                                                        table.Cell().Padding(3).AlignRight().Text($"{lineItem.Total:F2} €").FontSize(9);
                                                    }
                                                });
                                            }
                                        });
                                    }
                                }

                                // Total
                                column.Item().PaddingTop(20).AlignRight().Row(row =>
                                {
                                    row.ConstantItem(150).Text("YHTEENSÄ:").FontSize(14).Bold();
                                    row.ConstantItem(100).AlignRight().Text($"{invoice.Amount:F2} €").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.DefaultTextStyle(x => x.FontSize(9));
                                text.Span("Sivu ");
                                text.CurrentPageNumber();
                                text.Span(" / ");
                                text.TotalPages();
                            });
                    });
                });

                using var stream = new System.IO.MemoryStream();
                document.GeneratePdf(stream);
                var pdfBytes = stream.ToArray();
                _logger.LogInformation("Successfully generated full invoice PDF for invoice {InvoiceId}", invoiceId);
                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating full invoice PDF for invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task<byte[]> GenerateParticipantInvoicePdfAsync(Guid invoiceId, string participantId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Participants)
                        .ThenInclude(p => p.AppUser)
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.Organizer)
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.Payers)
                            .ThenInclude(p => p.AppUser)
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.LineItems)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    throw new Exception($"Invoice with id {invoiceId} not found");

                var participant = invoice.Participants?.FirstOrDefault(p => p.AppUserId == participantId);
                if (participant == null)
                    throw new Exception($"Participant with id {participantId} not found in invoice {invoiceId}");

                _logger.LogInformation("Generating participant invoice PDF for invoice {InvoiceId}, participant {ParticipantId}", invoiceId, participantId);

                // Calculate participant's share
                var participantExpenses = invoice.ExpenseItems?
                    .Where(ei => ei.Payers?.Any(p => p.AppUserId == participantId) ?? false)
                    .ToList() ?? new System.Collections.Generic.List<ExpenseItem>();

                decimal totalShare = 0;
                var shareDetails = new System.Collections.Generic.List<(string Name, decimal Amount, int PayerCount, decimal Share)>();

                foreach (var expenseItem in participantExpenses)
                {
                    var payerCount = expenseItem.Payers?.Count ?? 1;
                    var share = expenseItem.Amount / payerCount;
                    totalShare += share;
                    shareDetails.Add((expenseItem.Name, expenseItem.Amount, payerCount, share));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                        page.Header()
                            .Column(column =>
                            {
                                column.Item().Text($"Henkilökohtainen Lasku")
                                    .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text($"{invoice.Title} - {invoice.Description}")
                                    .FontSize(12).FontColor(Colors.Grey.Darken1);
                                column.Item().PaddingTop(10).Text($"Maksaja: {participant.AppUser?.DisplayName ?? "Unknown"}")
                                    .FontSize(14).Bold();
                                column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            });

                        page.Content()
                            .Column(column =>
                            {
                                column.Item().PaddingTop(10).Text("Maksuosuutesi").FontSize(14).Bold();

                                if (shareDetails.Any())
                                {
                                    column.Item().PaddingTop(10).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(3);
                                            columns.ConstantColumn(100);
                                            columns.ConstantColumn(80);
                                            columns.ConstantColumn(100);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Kulu").Bold();
                                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Kokonaissumma").Bold();
                                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Maksajia").Bold();
                                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Sinun osuutesi").Bold();
                                        });

                                        foreach (var detail in shareDetails)
                                        {
                                            table.Cell().Padding(5).Text(detail.Name);
                                            table.Cell().Padding(5).AlignRight().Text($"{detail.Amount:F2} €");
                                            table.Cell().Padding(5).AlignRight().Text($"{detail.PayerCount}");
                                            table.Cell().Padding(5).AlignRight().Text($"{detail.Share:F2} €");
                                        }
                                    });
                                }
                                else
                                {
                                    column.Item().Padding(10).Text("Ei kuluja tällä laskulla.").Italic();
                                }

                                // Total
                                column.Item().PaddingTop(20).Background(Colors.Blue.Lighten4).Padding(10).Row(row =>
                                {
                                    row.RelativeItem().Text("MAKSETTAVA YHTEENSÄ:").FontSize(16).Bold();
                                    row.ConstantItem(120).AlignRight().Text($"{totalShare:F2} €").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                                });

                                // Payment info
                                if (!string.IsNullOrEmpty(participant.AppUser?.BankAccount))
                                {
                                    column.Item().PaddingTop(20).Column(infoColumn =>
                                    {
                                        infoColumn.Item().Text("Maksutiedot").FontSize(12).Bold();
                                        infoColumn.Item().PaddingTop(5).Text($"Tilinumero: {participant.AppUser.BankAccount}");
                                    });
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.DefaultTextStyle(x => x.FontSize(9));
                                text.Span("Sivu ");
                                text.CurrentPageNumber();
                                text.Span(" / ");
                                text.TotalPages();
                            });
                    });
                });

                using var stream = new System.IO.MemoryStream();
                document.GeneratePdf(stream);
                var pdfBytes = stream.ToArray();
                _logger.LogInformation("Successfully generated participant invoice PDF for invoice {InvoiceId}, participant {ParticipantId}", invoiceId, participantId);
                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating participant invoice PDF for invoice {InvoiceId}, participant {ParticipantId}", invoiceId, participantId);
                throw;
            }
        }
    }
}
