using Xunit;
using Application.ExpenseItems;
using System.Threading;
using System.Threading.Tasks;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Domain;
using System;
using System.Linq;

namespace Application.UnitTests.ExpenseItems
{
    public class CreateTests
    {
        [Fact]
        public async Task Handle_ShouldCreateExpenseItem()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new DataContext(options))
            {
                var handler = new Create.Handler(context);
                var expenseItem = new ExpenseItem { Id = Guid.NewGuid(), Name = "New Item" };

                // Act
                await handler.Handle(new Create.Command { ExpenseItem = expenseItem }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var item = await context.ExpenseItems.FirstOrDefaultAsync();
                Assert.NotNull(item);
                Assert.Equal("New Item", item.Name);
            }
        }

        [Fact]
        public async Task Handle_ShouldAddExpenseItemToInvoice_WhenInvoiceIdProvided()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var invoiceId = Guid.NewGuid();
            var expenseItem = new ExpenseItem { Id = Guid.NewGuid(), Name = "Invoice Item" };

            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.Invoices.Add(new Invoice { Id = invoiceId, Title = "Invoice 1", ExpenseItems = new List<ExpenseItem>() });
                await context.SaveChangesAsync();
            }

            // Act - use new context to avoid tracking issues
            using (var context = new DataContext(options))
            {
                var handler = new Create.Handler(context);
                await handler.Handle(new Create.Command { ExpenseItem = expenseItem, InvoiceId = invoiceId }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var invoice = await context.Invoices.Include(i => i.ExpenseItems).FirstOrDefaultAsync(i => i.Id == invoiceId);
                Assert.NotNull(invoice);
                Assert.Single(invoice.ExpenseItems);
                Assert.Equal("Invoice Item", invoice.ExpenseItems.First().Name);
            }
        }

        [Fact]
        public async Task Handle_ShouldAddDefaultPayers_WhenInvoiceHasParticipants()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var invoiceId = Guid.NewGuid();
            var expenseItem = new ExpenseItem { Id = Guid.NewGuid(), Name = "Item with Payers" };

            // Seed with invoice and participants
            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.Creditors.AddRange(
                    new Creditor { Id = 1, Name = "Creditor 1", Email = "c1@example.com" },
                    new Creditor { Id = 2, Name = "Creditor 2", Email = "c2@example.com" },
                    new Creditor { Id = 3, Name = "Creditor 3", Email = "c3@example.com" }
                );
                var invoice = new Invoice
                {
                    Id = invoiceId,
                    Title = "Invoice with Participants",
                    Participants = new List<InvoiceParticipant>
                    {
                        new InvoiceParticipant { InvoiceId = invoiceId, CreditorId = 1 },
                        new InvoiceParticipant { InvoiceId = invoiceId, CreditorId = 2 },
                        new InvoiceParticipant { InvoiceId = invoiceId, CreditorId = 3 }
                    }
                };
                context.Invoices.Add(invoice);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new Create.Handler(context);
                await handler.Handle(new Create.Command { ExpenseItem = expenseItem, InvoiceId = invoiceId }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var savedItem = await context.ExpenseItems
                    .Include(ei => ei.Payers)
                    .FirstOrDefaultAsync(ei => ei.Id == expenseItem.Id);

                Assert.NotNull(savedItem);
                Assert.Equal(3, savedItem.Payers.Count);
                Assert.Contains(savedItem.Payers, p => p.CreditorId == 1);
                Assert.Contains(savedItem.Payers, p => p.CreditorId == 2);
                Assert.Contains(savedItem.Payers, p => p.CreditorId == 3);
            }
        }
    }
}
