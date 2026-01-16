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

                                // Calculate optimized payments for this invoice
                                var balances = CalculateParticipantBalances(invoice);
                                var allTransactions = OptimizePaymentTransactions(balances, invoice);

                                // Filter to show only transactions where this participant is the payer
                                var participantTransactions = allTransactions
                                    .Where(t => t.FromUserId == participantId)
                                    .ToList();

                                if (participantTransactions.Any())
                                {
                                    foreach (var transaction in participantTransactions)
                                    {
                                        column.Item().PaddingTop(10).Column(infoColumn =>
                                        {
                                            infoColumn.Item().Text($"Maksa {transaction.ToUserName}:lle {transaction.Amount:F2} €").FontSize(12).Bold();

                                            if (transaction.ToUser != null)
                                            {
                                                var hasBankAccount = !string.IsNullOrEmpty(transaction.ToUser.BankAccount);
                                                var hasPhoneNumber = !string.IsNullOrEmpty(transaction.ToUser.PhoneNumber);

                                                // Show preferred payment method first, then others
                                                if (transaction.ToUser.PreferredPaymentMethod == "Pankki" && hasBankAccount)
                                                {
                                                    infoColumn.Item().PaddingTop(3).Text($"Pankki: {transaction.ToUser.BankAccount} (Ensisijainen)").FontSize(10);
                                                    if (hasPhoneNumber)
                                                    {
                                                        infoColumn.Item().PaddingTop(2).Text($"MobilePay: {transaction.ToUser.PhoneNumber}").FontSize(10);
                                                    }
                                                }
                                                else if (transaction.ToUser.PreferredPaymentMethod == "MobilePay" && hasPhoneNumber)
                                                {
                                                    infoColumn.Item().PaddingTop(3).Text($"MobilePay: {transaction.ToUser.PhoneNumber} (Ensisijainen)").FontSize(10);
                                                    if (hasBankAccount)
                                                    {
                                                        infoColumn.Item().PaddingTop(2).Text($"Pankki: {transaction.ToUser.BankAccount}").FontSize(10);
                                                    }
                                                }
                                                else
                                                {
                                                    // No preferred method set, show both if available
                                                    if (hasBankAccount)
                                                    {
                                                        infoColumn.Item().PaddingTop(3).Text($"Pankki: {transaction.ToUser.BankAccount}").FontSize(10);
                                                    }
                                                    if (hasPhoneNumber)
                                                    {
                                                        infoColumn.Item().PaddingTop(2).Text($"MobilePay: {transaction.ToUser.PhoneNumber}").FontSize(10);
                                                    }
                                                    if (!hasBankAccount && !hasPhoneNumber)
                                                    {
                                                        infoColumn.Item().PaddingTop(3).Text("Ei maksutietoja saatavilla").FontSize(10).Italic();
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                infoColumn.Item().PaddingTop(3).Text("Ei maksutietoja saatavilla").FontSize(10).Italic();
                                            }
                                        });
                                    }
                                }
                                else
                                {
                                    column.Item().PaddingTop(10).Text("Sinulla ei ole maksettavia maksuja.").FontSize(10).Italic();
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
    }
}
