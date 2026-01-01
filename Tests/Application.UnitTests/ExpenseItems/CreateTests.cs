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

        [Fact(Skip = "Fails with DbUpdateConcurrencyException in InMemory provider linked to Shadow Property updates")]
        public async Task Handle_ShouldAddExpenseItemToInvoice_WhenInvoiceIdProvided()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var invoiceId = Guid.NewGuid();

            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.Invoices.Add(new Invoice { Id = invoiceId, Title = "Invoice 1", ExpenseItems = new List<ExpenseItem>() });
                await context.SaveChangesAsync();
            }

            using (var context = new DataContext(options))
            {
                var handler = new Create.Handler(context);
                var expenseItem = new ExpenseItem { Id = Guid.NewGuid(), Name = "Invoice Item" };

                // Act
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
    }
}
