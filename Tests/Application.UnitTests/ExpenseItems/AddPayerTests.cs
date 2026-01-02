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
    public class AddPayerTests
    {
        [Fact]
        public async Task Handle_ShouldAddPayerToExpenseItem()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseItemId = Guid.NewGuid();
            var creditorId = 1;

            // Seed data
            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.ExpenseItems.Add(new ExpenseItem
                {
                    Id = expenseItemId,
                    Name = "Test Item",
                    Payers = new List<ExpenseItemPayer>()
                });
                context.Creditors.Add(new Creditor
                {
                    Id = creditorId,
                    Name = "Test Creditor",
                    Email = "test@example.com"
                });
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new AddPayer.Handler(context);
                await handler.Handle(new AddPayer.Command
                {
                    ExpenseItemId = expenseItemId,
                    CreditorId = creditorId
                }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var payer = await context.ExpenseItemPayers
                    .FirstOrDefaultAsync(p => p.ExpenseItemId == expenseItemId && p.CreditorId == creditorId);
                Assert.NotNull(payer);
                Assert.Equal(expenseItemId, payer.ExpenseItemId);
                Assert.Equal(creditorId, payer.CreditorId);
            }
        }

        [Fact]
        public async Task Handle_ShouldNotAddDuplicatePayer()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseItemId = Guid.NewGuid();
            var creditorId = 1;

            // Seed with existing payer
            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                var expenseItem = new ExpenseItem
                {
                    Id = expenseItemId,
                    Name = "Test Item"
                };
                context.ExpenseItems.Add(expenseItem);
                context.Creditors.Add(new Creditor
                {
                    Id = creditorId,
                    Name = "Test Creditor",
                    Email = "test@example.com"
                });
                context.ExpenseItemPayers.Add(new ExpenseItemPayer
                {
                    ExpenseItemId = expenseItemId,
                    CreditorId = creditorId
                });
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new AddPayer.Handler(context);
                await handler.Handle(new AddPayer.Command
                {
                    ExpenseItemId = expenseItemId,
                    CreditorId = creditorId
                }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var payerCount = await context.ExpenseItemPayers
                    .CountAsync(p => p.ExpenseItemId == expenseItemId && p.CreditorId == creditorId);
                Assert.Equal(1, payerCount); // Should still be only 1
            }
        }
    }
}
