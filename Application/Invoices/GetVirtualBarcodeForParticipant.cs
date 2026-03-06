using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Invoices
{
    public class GetVirtualBarcodeForParticipant
    {
        public class Query : IRequest<VirtualBarcodeDto>
        {
            public Guid InvoiceId { get; set; }
            public string UserId { get; set; }
        }

        public class VirtualBarcodeDto
        {
            public string Barcode { get; set; }
            public string ReferenceNumber { get; set; }
            public string FormattedReference { get; set; }
            public string RecipientName { get; set; }
            public string RecipientBankAccount { get; set; }
            public decimal Amount { get; set; }
            public DateTime DueDate { get; set; }
        }

        public class Handler : IRequestHandler<Query, VirtualBarcodeDto>
        {
            private readonly DataContext _context;
            private readonly VirtualBarcodeService _barcodeService;

            public Handler(DataContext context, VirtualBarcodeService barcodeService)
            {
                _context = context;
                _barcodeService = barcodeService;
            }

            public async Task<VirtualBarcodeDto> Handle(Query request, CancellationToken cancellationToken)
            {
                // Load invoice with all necessary data
                var invoice = await _context.Invoices
                    .Include(i => i.Participants)
                        .ThenInclude(p => p.AppUser)
                    .Include(i => i.ExpenseItems)
                        .ThenInclude(ei => ei.Payers)
                    .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

                if (invoice == null)
                    throw new Exception($"Invoice with id {request.InvoiceId} not found");

                // Calculate participant balance and find payment transaction
                var balances = CalculateParticipantBalances(invoice);
                var transactions = OptimizePaymentTransactions(balances, invoice);

                // Find the transaction where this user is the payer
                var participantTransaction = transactions.FirstOrDefault(t => t.FromUserId == request.UserId);

                if (participantTransaction == null)
                    return null; // No payment needed for this participant

                // Generate reference number
                var referenceBase = $"{invoice.LanNumber}{request.UserId.Substring(0, Math.Min(6, request.UserId.Length))}";
                var referenceNumber = _barcodeService.GenerateReferenceNumberWithCheckDigit(referenceBase);
                var formattedReference = _barcodeService.FormatReferenceNumber(referenceNumber);

                // Generate virtual barcode
                var dueDate = DateTime.Now.AddDays(14);
                var virtualBarcode = _barcodeService.GenerateVirtualBarcode(
                    participantTransaction.ToUser.BankAccount,
                    participantTransaction.Amount,
                    referenceNumber,
                    dueDate
                );

                return new VirtualBarcodeDto
                {
                    Barcode = virtualBarcode,
                    ReferenceNumber = referenceNumber,
                    FormattedReference = formattedReference,
                    RecipientName = participantTransaction.ToUserName,
                    RecipientBankAccount = participantTransaction.ToUser.BankAccount,
                    Amount = participantTransaction.Amount,
                    DueDate = dueDate
                };
            }

            // Helper methods copied from PdfService
            private ParticipantBalance[] CalculateParticipantBalances(Invoice invoice)
            {
                var balances = new System.Collections.Generic.Dictionary<string, ParticipantBalance>();

                if (invoice.Participants == null || invoice.ExpenseItems == null)
                    return Array.Empty<ParticipantBalance>();

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

                foreach (var expenseItem in invoice.ExpenseItems)
                {
                    var totalAmount = expenseItem.Amount;
                    var payerCount = expenseItem.Payers?.Count ?? 0;

                    if (payerCount == 0 || totalAmount == null || double.IsNaN((double)totalAmount))
                        continue;

                    var sharePerPerson = totalAmount / payerCount;

                    if (balances.ContainsKey(expenseItem.OrganizerId))
                        balances[expenseItem.OrganizerId].TotalPaid += totalAmount;

                    if (expenseItem.Payers != null)
                    {
                        foreach (var payer in expenseItem.Payers)
                        {
                            if (balances.ContainsKey(payer.AppUserId))
                                balances[payer.AppUserId].TotalOwed += sharePerPerson;
                        }
                    }
                }

                foreach (var balance in balances.Values)
                {
                    balance.NetBalance = balance.TotalOwed - balance.TotalPaid;
                }

                return balances.Values.OrderBy(b => b.NetBalance).ToArray();
            }

            private PaymentTransaction[] OptimizePaymentTransactions(ParticipantBalance[] balances, Invoice invoice)
            {
                var transactions = new System.Collections.Generic.List<PaymentTransaction>();
                const decimal epsilon = 0.01m;

                var debtors = balances.Where(b => b.NetBalance > epsilon)
                    .Select(b => new { b.UserId, b.DisplayName, Amount = b.NetBalance })
                    .ToList();

                var creditors = balances.Where(b => b.NetBalance < -epsilon)
                    .Select(b => new { b.UserId, b.DisplayName, Amount = -b.NetBalance })
                    .OrderByDescending(c => c.Amount)
                    .ToList();

                if (!creditors.Any() || !debtors.Any())
                    return Array.Empty<PaymentTransaction>();

                var mainCreditor = creditors.First();
                var mainCreditorUser = invoice.Participants?.FirstOrDefault(p => p.AppUserId == mainCreditor.UserId)?.AppUser;
                var otherCreditors = creditors.Skip(1).ToList();

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

                return transactions.ToArray();
            }
        }

        // Helper classes
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
    }
}
