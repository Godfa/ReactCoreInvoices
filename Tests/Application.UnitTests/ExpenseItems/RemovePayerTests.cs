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
    public class RemovePayerTests
    {
        [Fact]
        public async Task Handle_ShouldRemovePayerFromExpenseItem()
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
                context.ExpenseItems.Add(new ExpenseItem
                {
                    Id = expenseItemId,
                    Name = "Test Item"
                });
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
                var handler = new RemovePayer.Handler(context);
                await handler.Handle(new RemovePayer.Command
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
                Assert.Null(payer);
            }
        }

        [Fact]
        public async Task Handle_ShouldHandleRemovingNonExistentPayer()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseItemId = Guid.NewGuid();
            var creditorId = 1;

            // Seed without payer
            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.ExpenseItems.Add(new ExpenseItem
                {
                    Id = expenseItemId,
                    Name = "Test Item"
                });
                context.Creditors.Add(new Creditor
                {
                    Id = creditorId,
                    Name = "Test Creditor",
                    Email = "test@example.com"
                });
                await context.SaveChangesAsync();
            }

            // Act - should not throw
            using (var context = new DataContext(options))
            {
                var handler = new RemovePayer.Handler(context);
                await handler.Handle(new RemovePayer.Command
                {
                    ExpenseItemId = expenseItemId,
                    CreditorId = creditorId
                }, CancellationToken.None);
            }

            // Assert - no exception thrown
            using (var context = new DataContext(options))
            {
                var payerCount = await context.ExpenseItemPayers.CountAsync();
                Assert.Equal(0, payerCount);
            }
        }
    }
}
