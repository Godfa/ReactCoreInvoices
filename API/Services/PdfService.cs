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
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.SkiaSharp;
using SkiaSharp;

namespace API.Services
{
    public class PdfService : IPdfService
    {
        private readonly DataContext _context;
        private readonly ILogger<PdfService> _logger;
        private readonly VirtualBarcodeService _barcodeService;

        public PdfService(DataContext context, ILogger<PdfService> logger, VirtualBarcodeService barcodeService)
        {
            _context = context;
            _logger = logger;
            _barcodeService = barcodeService;

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
                decimal totalPaidByParticipant = 0;
                var shareDetails = new System.Collections.Generic.List<(string Name, decimal Amount, int PayerCount, decimal Share, string OrganizerName, bool PaidBySelf)>();

                foreach (var expenseItem in participantExpenses)
                {
                    var payerCount = expenseItem.Payers?.Count ?? 1;
                    var share = expenseItem.Amount / payerCount;
                    totalShare += share;

                    var organizerName = expenseItem.Organizer?.DisplayName ?? "Tuntematon";
                    var paidBySelf = expenseItem.Organizer?.Id == participantId;

                    if (paidBySelf)
                    {
                        totalPaidByParticipant += expenseItem.Amount;
                    }

                    shareDetails.Add((expenseItem.Name, expenseItem.Amount, payerCount, share, organizerName, paidBySelf));
                }

                // Calculate optimized payments for this invoice
                var balances = CalculateParticipantBalances(invoice);
                var allTransactions = OptimizePaymentTransactions(balances, invoice);

                // Filter transactions: outgoing (participant pays) and incoming (participant receives)
                var outgoingTransactions = allTransactions
                    .Where(t => t.FromUserId == participantId)
                    .ToList();

                var incomingTransactions = allTransactions
                    .Where(t => t.ToUserId == participantId)
                    .ToList();

                // Calculate net payment (positive = owes, negative = is owed)
                var totalOutgoing = outgoingTransactions.Sum(t => t.Amount);
                var totalIncoming = incomingTransactions.Sum(t => t.Amount);
                var netPayment = totalOutgoing - totalIncoming;

                // For backwards compatibility, use outgoing transactions as primary
                var participantTransactions = outgoingTransactions;

                // Generate reference number and virtual barcode (if bank account available)
                var referenceBase = _barcodeService.GenerateReferenceFromInvoiceId(invoiceId);
                var referenceNumber = _barcodeService.GenerateReferenceNumberWithCheckDigit(referenceBase);
                var formattedReference = _barcodeService.FormatReferenceNumber(referenceNumber);

                string virtualBarcode = null;
                byte[] barcodeImageBytes = null;
                byte[] qrCodeBytes = null;

                // Generate virtual barcode and QR code only if participant owes money
                if (netPayment > 0.01m && outgoingTransactions.Any())
                {
                    var transaction = outgoingTransactions.First();
                    if (transaction.ToUser != null && !string.IsNullOrEmpty(transaction.ToUser.BankAccount))
                    {
                        try
                        {
                            var dueDate = DateTime.Now.AddDays(14);
                            virtualBarcode = _barcodeService.GenerateVirtualBarcode(
                                transaction.ToUser.BankAccount,
                                transaction.Amount,
                                referenceNumber,
                                dueDate
                            );

                            // Log barcode details for debugging
                            _logger.LogInformation("Generated virtual barcode: {Barcode} (length: {Length})",
                                virtualBarcode, virtualBarcode.Length);
                            _logger.LogInformation("Barcode components - IBAN: {IBAN}, Amount: {Amount}, Reference: {Reference}, DueDate: {DueDate}",
                                transaction.ToUser.BankAccount, transaction.Amount, referenceNumber, dueDate.ToString("dd.MM.yyyy"));

                            barcodeImageBytes = GenerateBarcodeImage(virtualBarcode, 500, 80);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate virtual barcode");
                        }
                    }

                    // Generate QR code for MobilePay if phone number available
                    if (!string.IsNullOrEmpty(transaction.ToUser?.PhoneNumber))
                    {
                        try
                        {
                            var qrMessage = $"{invoice.Title} - {participant.AppUser?.DisplayName}";
                            qrCodeBytes = GenerateQRCode(transaction.ToUser.PhoneNumber, transaction.Amount, qrMessage, 150);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate QR code");
                        }
                    }
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        // === HEADER SECTION (Helen-style) ===
                        page.Header().Column(header =>
                        {
                            // Top row: Logo/Title + Invoice info box
                            header.Item().Row(row =>
                            {
                                // Left: Title
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("MOKKILAN LASKUT")
                                        .FontSize(18).Bold()
                                        .FontColor("#0066CC");
                                    col.Item().PaddingTop(2).Text("Henkilökohtainen lasku")
                                        .FontSize(10).FontColor("#666666");
                                });

                                // Right: Invoice info box
                                row.ConstantItem(200).Border(1).BorderColor("#CCCCCC")
                                    .Background("#F8F8F8")
                                    .Padding(8).Column(col =>
                                {
                                    col.Item().Text("LASKUN TIEDOT")
                                        .FontSize(8).Bold().FontColor("#666666");
                                    col.Item().PaddingTop(3).Row(r =>
                                    {
                                        r.RelativeItem().Text("Laskun numero:").FontSize(8);
                                        r.ConstantItem(80).AlignRight().Text(invoiceId.ToString().Substring(0, 8).ToUpper()).FontSize(8).Bold();
                                    });
                                    col.Item().PaddingTop(1).Row(r =>
                                    {
                                        r.RelativeItem().Text("Eräpäivä:").FontSize(8);
                                        r.ConstantItem(80).AlignRight().Text($"{DateTime.Now.AddDays(14).ToString("dd.MM.yyyy")}").FontSize(8).Bold();
                                    });
                                    col.Item().PaddingTop(1).Row(r =>
                                    {
                                        r.RelativeItem().Text("Viite:").FontSize(8);
                                        r.ConstantItem(80).AlignRight().Text(formattedReference).FontSize(7);
                                    });
                                });
                            });

                            header.Item().PaddingTop(8);
                        });

                        // === CONTENT SECTION ===
                        page.Content().Column(content =>
                        {
                            // Expenses table header
                            content.Item().Text("KULUERITTELY").FontSize(11).Bold().FontColor("#0066CC");
                            content.Item().PaddingTop(5);

                            // Expenses table
                            if (shareDetails.Any())
                            {
                                content.Item().Border(1).BorderColor("#CCCCCC").Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(3);
                                        cols.ConstantColumn(100);
                                        cols.ConstantColumn(90);
                                        cols.ConstantColumn(60);
                                        cols.ConstantColumn(90);
                                    });

                                    // Table header with blue background
                                    table.Header(h =>
                                    {
                                        h.Cell().Background("#0066CC").Padding(6)
                                            .Text("Kulu").FontSize(9).Bold().FontColor("#FFFFFF");
                                        h.Cell().Background("#0066CC").Padding(6)
                                            .Text("Maksaja").FontSize(9).Bold().FontColor("#FFFFFF");
                                        h.Cell().Background("#0066CC").Padding(6).AlignRight()
                                            .Text("Kokonaissumma").FontSize(9).Bold().FontColor("#FFFFFF");
                                        h.Cell().Background("#0066CC").Padding(6).AlignCenter()
                                            .Text("Maksajia").FontSize(9).Bold().FontColor("#FFFFFF");
                                        h.Cell().Background("#0066CC").Padding(6).AlignRight()
                                            .Text("Sinun osuutesi").FontSize(9).Bold().FontColor("#FFFFFF");
                                    });

                                    // Expense rows with alternating background
                                    bool alternate = false;
                                    foreach (var detail in shareDetails)
                                    {
                                        var bgColor = alternate ? "#F8F8F8" : "#FFFFFF";

                                        table.Cell().Background(bgColor).Padding(6).Text(detail.Name).FontSize(9);

                                        // Payer name - highlight if paid by self
                                        var payerColor = detail.PaidBySelf ? "#00AA00" : "#333333";
                                        table.Cell().Background(bgColor).Padding(6).Text(detail.OrganizerName)
                                            .FontSize(9).FontColor(payerColor);
                                        if (detail.PaidBySelf)
                                        {
                                            table.Cell().Background(bgColor).Padding(6).AlignRight()
                                                .Text($"{detail.Amount:F2} €").FontSize(9).Bold().FontColor(payerColor);
                                        }
                                        else
                                        {
                                            table.Cell().Background(bgColor).Padding(6).AlignRight()
                                                .Text($"{detail.Amount:F2} €").FontSize(9);
                                        }

                                        table.Cell().Background(bgColor).Padding(6).AlignCenter().Text($"{detail.PayerCount}").FontSize(9);
                                        table.Cell().Background(bgColor).Padding(6).AlignRight().Text($"{detail.Share:F2} €")
                                            .FontSize(9).Bold().FontColor("#0066CC");

                                        alternate = !alternate;
                                    }
                                });

                                // Summary of items paid by participant
                                if (totalPaidByParticipant > 0)
                                {
                                    content.Item().PaddingTop(8).Row(row =>
                                    {
                                        row.RelativeItem().Text("Itse maksamasi kulut yhteensä:")
                                            .FontSize(9).Bold().FontColor("#666666");
                                        row.ConstantItem(90).AlignRight().Text($"{totalPaidByParticipant:F2} €")
                                            .FontSize(10).Bold().FontColor("#00AA00");
                                    });
                                }
                            }
                            else
                            {
                                content.Item().Border(1).BorderColor("#CCCCCC").Padding(15)
                                    .Text("Ei kuluja tällä laskulla.").FontSize(10).Italic().FontColor("#999999");
                            }

                            // Total amount boxes - show both share and actual payment
                            content.Item().PaddingTop(12).Row(row =>
                            {
                                // Left: Total share (informational)
                                row.RelativeItem().Border(1).BorderColor("#CCCCCC")
                                    .Background("#F8F8F8")
                                    .Padding(8).Column(col =>
                                {
                                    col.Item().Text("KULUOSUUTESI")
                                        .FontSize(9).Bold().FontColor("#666666");
                                    col.Item().PaddingTop(3).Text($"{totalShare:F2} €")
                                        .FontSize(16).Bold().FontColor("#333333");
                                    col.Item().PaddingTop(1).Text("(Osuutesi kaikista kuluista)")
                                        .FontSize(7).Italic().FontColor("#999999");
                                });

                                row.ConstantItem(10); // Spacer

                                // Right: Net payment (positive = owes, negative = is owed, zero = balanced)
                                if (netPayment > 0.01m) // Owes money
                                {
                                    row.RelativeItem().Border(2).BorderColor("#0066CC")
                                        .Background("#E6F2FF")
                                        .Padding(10).Column(col =>
                                    {
                                        col.Item().Text("MAKSETTAVA")
                                            .FontSize(10).Bold().FontColor("#666666");
                                        col.Item().PaddingTop(3).Text($"{netPayment:F2} €")
                                            .FontSize(22).Bold().FontColor("#0066CC");
                                        col.Item().PaddingTop(1).Text("(Osuutesi miinus jo maksamasi)")
                                            .FontSize(7).Italic().FontColor("#666666");
                                    });
                                }
                                else if (netPayment < -0.01m) // Is owed money
                                {
                                    row.RelativeItem().Border(2).BorderColor("#00AA00")
                                        .Background("#E6FFE6")
                                        .Padding(10).Column(col =>
                                    {
                                        col.Item().Text("SAATAVAT")
                                            .FontSize(10).Bold().FontColor("#666666");
                                        col.Item().PaddingTop(3).Text($"{Math.Abs(netPayment):F2} €")
                                            .FontSize(22).Bold().FontColor("#00AA00");
                                        col.Item().PaddingTop(1).Text("(Maksamasi miinus osuutesi)")
                                            .FontSize(7).Italic().FontColor("#666666");
                                    });
                                }
                                else // Balanced
                                {
                                    row.RelativeItem().Border(1).BorderColor("#CCCCCC")
                                        .Background("#F0F0F0")
                                        .Padding(10).Column(col =>
                                    {
                                        col.Item().Text("TASAPAINOSSA")
                                            .FontSize(10).Bold().FontColor("#666666");
                                        col.Item().PaddingTop(3).Text("0,00 €")
                                            .FontSize(22).Bold().FontColor("#666666");
                                        col.Item().PaddingTop(1).Text("(Ei maksettavaa)")
                                            .FontSize(7).Italic().FontColor("#999999");
                                    });
                                }
                            });

                            // Show receivables first if participant is owed money
                            if (incomingTransactions.Any())
                            {
                                content.Item().PaddingTop(15).Column(col =>
                                {
                                    col.Item().Text("SAATAVAT - SINULLE MAKSAVAT").FontSize(11).Bold().FontColor("#00AA00");
                                    col.Item().PaddingTop(5);

                                    // Receivables table (same style as expenses table)
                                    col.Item().Border(1).BorderColor("#CCCCCC").Table(table =>
                                    {
                                        table.ColumnsDefinition(cols =>
                                        {
                                            cols.RelativeColumn(3);
                                            cols.ConstantColumn(120);
                                        });

                                        // Table header with green background
                                        table.Header(h =>
                                        {
                                            h.Cell().Background("#00AA00").Padding(6)
                                                .Text("Maksaja").FontSize(9).Bold().FontColor("#FFFFFF");
                                            h.Cell().Background("#00AA00").Padding(6).AlignRight()
                                                .Text("Summa").FontSize(9).Bold().FontColor("#FFFFFF");
                                        });

                                        // Receivable rows with alternating background
                                        bool alternate = false;
                                        foreach (var transaction in incomingTransactions)
                                        {
                                            var bgColor = alternate ? "#F8F8F8" : "#FFFFFF";

                                            table.Cell().Background(bgColor).Padding(6).Text(transaction.FromUserName).FontSize(9);
                                            table.Cell().Background(bgColor).Padding(6).AlignRight().Text($"{transaction.Amount:F2} €")
                                                .FontSize(9).Bold().FontColor("#00AA00");

                                            alternate = !alternate;
                                        }
                                    });

                                    // Info text below table
                                    col.Item().PaddingTop(8).Text("Maksaja saa oman laskun, jossa on sinun maksutietosi.")
                                        .FontSize(9).Italic().FontColor("#666666");
                                });
                            }

                            // Payment info section - show after receivables if participant has outgoing payments
                            if (outgoingTransactions.Any())
                            {
                                foreach (var transaction in outgoingTransactions)
                                {
                                    content.Item().PaddingTop(15).Border(1).BorderColor("#CCCCCC")
                                        .Column(col =>
                                    {
                                        col.Item().Background("#0066CC").Padding(8)
                                            .Text("MAKSUTIEDOT").FontSize(10).Bold().FontColor("#FFFFFF");

                                        col.Item().Padding(12).Column(paymentCol =>
                                        {
                                            // Recipient
                                            paymentCol.Item().Row(r =>
                                            {
                                                r.ConstantItem(110).Text("Saaja:").FontSize(9).FontColor("#666666");
                                                r.RelativeItem().Text(transaction.ToUserName).FontSize(10).Bold();
                                            });

                                            // Bank account
                                            if (!string.IsNullOrEmpty(transaction.ToUser?.BankAccount))
                                            {
                                                paymentCol.Item().PaddingTop(4).Row(r =>
                                                {
                                                    r.ConstantItem(110).Text("Tilinumero:").FontSize(9).FontColor("#666666");
                                                    r.RelativeItem().Text(transaction.ToUser.BankAccount).FontSize(10).Bold();
                                                    if (transaction.ToUser.PreferredPaymentMethod == "Pankki")
                                                    {
                                                        r.ConstantItem(90).AlignRight().Text("(Ensisijainen)")
                                                            .FontSize(8).FontColor("#0066CC").Italic();
                                                    }
                                                });
                                            }

                                            // Reference number
                                            paymentCol.Item().PaddingTop(4).Row(r =>
                                            {
                                                r.ConstantItem(110).Text("Viitenumero:").FontSize(9).FontColor("#666666");
                                                r.RelativeItem().Text(formattedReference).FontSize(10).Bold();
                                            });

                                            // Due date
                                            paymentCol.Item().PaddingTop(4).Row(r =>
                                            {
                                                r.ConstantItem(110).Text("Eräpäivä:").FontSize(9).FontColor("#666666");
                                                r.RelativeItem().Text($"{DateTime.Now.AddDays(14).ToString("dd.MM.yyyy")}")
                                                    .FontSize(10).Bold();
                                            });

                                            // Amount
                                            paymentCol.Item().PaddingTop(4).Row(r =>
                                            {
                                                r.ConstantItem(110).Text("Summa:").FontSize(9).FontColor("#666666");
                                                r.RelativeItem().Text($"{transaction.Amount:F2} €").FontSize(12).Bold().FontColor("#0066CC");
                                            });

                                            // MobilePay section with QR code
                                            if (!string.IsNullOrEmpty(transaction.ToUser?.PhoneNumber))
                                            {
                                                paymentCol.Item().PaddingTop(8).LineHorizontal(1).LineColor("#EEEEEE");
                                                paymentCol.Item().PaddingTop(8).Row(r =>
                                                {
                                                    r.RelativeItem().Column(mpCol =>
                                                    {
                                                        mpCol.Item().Text("MobilePay").FontSize(10).Bold();
                                                        if (transaction.ToUser.PreferredPaymentMethod == "MobilePay")
                                                        {
                                                            mpCol.Item().Text("(Ensisijainen)")
                                                                .FontSize(8).FontColor("#0066CC").Italic();
                                                        }
                                                        mpCol.Item().PaddingTop(3).Text($"Numero: {transaction.ToUser.PhoneNumber}")
                                                            .FontSize(9);
                                                        mpCol.Item().PaddingTop(2).Text($"Summa: {transaction.Amount:F2} €")
                                                            .FontSize(9).Bold();
                                                        if (qrCodeBytes != null)
                                                        {
                                                            mpCol.Item().PaddingTop(3).Text("← Skannaa")
                                                                .FontSize(8).FontColor("#666666").Italic();
                                                        }
                                                    });

                                                    if (qrCodeBytes != null)
                                                    {
                                                        r.ConstantItem(100).AlignRight()
                                                            .Border(1).BorderColor("#CCCCCC")
                                                            .Padding(4).Height(100).Width(100)
                                                            .Image(qrCodeBytes);
                                                    }
                                                });
                                            }
                                        });

                                        // Virtual barcode section (if available)
                                        if (virtualBarcode != null && barcodeImageBytes != null)
                                        {
                                            col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#EEEEEE");
                                            col.Item().PaddingTop(8).Column(barcodeCol =>
                                            {
                                                barcodeCol.Item().Text("VIRTUAALIVIIVAKOODI").FontSize(8).Bold().FontColor("#666666");
                                                barcodeCol.Item().PaddingTop(2).Text(virtualBarcode)
                                                    .FontSize(7).FontFamily("Courier New").FontColor("#333333");
                                                barcodeCol.Item().PaddingTop(3).Height(50)
                                                    .Image(barcodeImageBytes);
                                            });
                                        }
                                    });
                                }
                            }

                            // Show "no transactions" message only if participant has neither payments nor receivables
                            if (!outgoingTransactions.Any() && !incomingTransactions.Any())
                            {
                                content.Item().PaddingTop(15).Border(1).BorderColor("#CCCCCC")
                                    .Padding(15).Background("#F8F8F8")
                                    .Text("Sinulla ei ole maksettavia maksuja tai saatavia tällä laskulla.")
                                    .FontSize(10).Italic().FontColor("#999999");
                            }
                        });

                        // === FOOTER SECTION ===
                        page.Footer().Column(footer =>
                        {
                            // Page number
                            footer.Item().AlignCenter().Text(t =>
                            {
                                t.DefaultTextStyle(x => x.FontSize(8).FontColor("#999999"));
                                t.Span("Sivu ");
                                t.CurrentPageNumber();
                                t.Span(" / ");
                                t.TotalPages();
                            });
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

        // Helper classes and methods for payment optimization
        private class ParticipantBalance
        {
            public string UserId { get; set; }
            public string DisplayName { get; set; }
            public decimal TotalPaid { get; set; }
            public decimal TotalOwed { get; set; }
            public decimal NetBalance { get; set; }
        }

        private class PaymentTransaction
        {
            public string FromUserId { get; set; }
            public string FromUserName { get; set; }
            public string ToUserId { get; set; }
            public string ToUserName { get; set; }
            public decimal Amount { get; set; }
            public User ToUser { get; set; }
        }

        private System.Collections.Generic.List<ParticipantBalance> CalculateParticipantBalances(Invoice invoice)
        {
            var balances = new System.Collections.Generic.Dictionary<string, ParticipantBalance>();

            if (invoice.Participants == null || invoice.ExpenseItems == null)
            {
                return new System.Collections.Generic.List<ParticipantBalance>();
            }

            // Initialize all participants
            foreach (var participant in invoice.Participants)
            {
                balances[participant.AppUserId] = new ParticipantBalance
                {
                    UserId = participant.AppUserId,
                    DisplayName = participant.AppUser?.DisplayName ?? "Unknown",
                    TotalPaid = 0,
                    TotalOwed = 0,
                    NetBalance = 0
                };
            }

            // Calculate paid and owed amounts
            foreach (var expenseItem in invoice.ExpenseItems)
            {
                var totalAmount = expenseItem.Amount;
                var payerCount = expenseItem.Payers?.Count ?? 0;

                if (payerCount == 0) continue;

                var sharePerPerson = totalAmount / payerCount;

                // Add paid amount to organizer
                if (balances.ContainsKey(expenseItem.OrganizerId))
                {
                    balances[expenseItem.OrganizerId].TotalPaid += totalAmount;
                }

                // Add owed amount to each payer
                foreach (var payer in expenseItem.Payers ?? new System.Collections.Generic.List<ExpenseItemPayer>())
                {
                    if (balances.ContainsKey(payer.AppUserId))
                    {
                        balances[payer.AppUserId].TotalOwed += sharePerPerson;
                    }
                }
            }

            // Calculate net balances
            foreach (var balance in balances.Values)
            {
                // Negative = paid more than owed = others owe this person
                // Positive = owes more than paid = owes to others
                balance.NetBalance = balance.TotalOwed - balance.TotalPaid;
            }

            return balances.Values.OrderBy(b => b.NetBalance).ToList();
        }

        private System.Collections.Generic.List<PaymentTransaction> OptimizePaymentTransactions(
            System.Collections.Generic.List<ParticipantBalance> balances,
            Invoice invoice)
        {
            var transactions = new System.Collections.Generic.List<PaymentTransaction>();
            const decimal epsilon = 0.01m;

            // Separate debtors and creditors
            var debtors = balances
                .Where(b => b.NetBalance > epsilon)
                .Select(b => new { b.UserId, b.DisplayName, Amount = b.NetBalance })
                .ToList();

            var creditors = balances
                .Where(b => b.NetBalance < -epsilon)
                .Select(b => new { b.UserId, b.DisplayName, Amount = -b.NetBalance })
                .OrderByDescending(c => c.Amount)
                .ToList();

            if (creditors.Count == 0 || debtors.Count == 0)
            {
                return transactions;
            }

            // Main creditor is the "intermediary"
            var mainCreditor = creditors[0];
            var mainCreditorUser = invoice.Participants?.FirstOrDefault(p => p.AppUserId == mainCreditor.UserId)?.AppUser;
            var otherCreditors = creditors.Skip(1).ToList();

            // 1. All debtors pay to the main creditor
            foreach (var debtor in debtors)
            {
                transactions.Add(new PaymentTransaction
                {
                    FromUserId = debtor.UserId,
                    FromUserName = debtor.DisplayName,
                    ToUserId = mainCreditor.UserId,
                    ToUserName = mainCreditor.DisplayName,
                    Amount = debtor.Amount,
                    ToUser = mainCreditorUser
                });
            }

            // 2. Main creditor pays to other creditors
            foreach (var creditor in otherCreditors)
            {
                var creditorUser = invoice.Participants?.FirstOrDefault(p => p.AppUserId == creditor.UserId)?.AppUser;
                transactions.Add(new PaymentTransaction
                {
                    FromUserId = mainCreditor.UserId,
                    FromUserName = mainCreditor.DisplayName,
                    ToUserId = creditor.UserId,
                    ToUserName = creditor.DisplayName,
                    Amount = creditor.Amount,
                    ToUser = creditorUser
                });
            }

            return transactions;
        }

        /// <summary>
        /// Generates Code 128 barcode image from virtual barcode string (cross-platform using SkiaSharp)
        /// </summary>
        private byte[] GenerateBarcodeImage(string virtualBarcode, int width = 600, int height = 100)
        {
            var writer = new BarcodeWriterGeneric
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = height,
                    Width = width,
                    Margin = 0,
                    PureBarcode = false
                }
            };

            var bitMatrix = writer.Encode(virtualBarcode);

            // Create SKBitmap from bit matrix
            using var bitmap = new SKBitmap(bitMatrix.Width, bitMatrix.Height);
            for (int y = 0; y < bitMatrix.Height; y++)
            {
                for (int x = 0; x < bitMatrix.Width; x++)
                {
                    var color = bitMatrix[x, y] ? SKColors.Black : SKColors.White;
                    bitmap.SetPixel(x, y, color);
                }
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            return data.ToArray();
        }

        /// <summary>
        /// Generates QR code with MobilePay payment information (cross-platform using SkiaSharp)
        /// </summary>
        private byte[] GenerateQRCode(string phoneNumber, decimal amount, string message, int size = 200)
        {
            // MobilePay payment URL format
            var qrContent = $"mobilepay://send?phone={phoneNumber}&amount={amount:F2}&comment={Uri.EscapeDataString(message)}";

            var writer = new BarcodeWriterGeneric
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = size,
                    Width = size,
                    Margin = 1
                }
            };

            var bitMatrix = writer.Encode(qrContent);

            // Create SKBitmap from bit matrix
            using var bitmap = new SKBitmap(bitMatrix.Width, bitMatrix.Height);
            for (int y = 0; y < bitMatrix.Height; y++)
            {
                for (int x = 0; x < bitMatrix.Width; x++)
                {
                    var color = bitMatrix[x, y] ? SKColors.Black : SKColors.White;
                    bitmap.SetPixel(x, y, color);
                }
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            return data.ToArray();
        }
    }
}
