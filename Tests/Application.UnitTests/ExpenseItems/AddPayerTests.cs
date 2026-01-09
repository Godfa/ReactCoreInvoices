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
            var userId = Guid.NewGuid().ToString();

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
                context.Users.Add(new User
                {
                    Id = userId,
                    DisplayName = "Test User",
                    Email = "test@example.com",
                    UserName = "testuser"
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
                    AppUserId = userId
                }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var payer = await context.ExpenseItemPayers
                    .FirstOrDefaultAsync(p => p.ExpenseItemId == expenseItemId && p.AppUserId == userId);
                Assert.NotNull(payer);
                Assert.Equal(expenseItemId, payer.ExpenseItemId);
                Assert.Equal(userId, payer.AppUserId);
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
            var userId = Guid.NewGuid().ToString();

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
                context.Users.Add(new User
                {
                    Id = userId,
                    DisplayName = "Test User",
                    Email = "test@example.com",
                    UserName = "testuser"
                });
                context.ExpenseItemPayers.Add(new ExpenseItemPayer
                {
                    ExpenseItemId = expenseItemId,
                    AppUserId = userId
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
                    AppUserId = userId
                }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var payerCount = await context.ExpenseItemPayers
                    .CountAsync(p => p.ExpenseItemId == expenseItemId && p.AppUserId == userId);
                Assert.Equal(1, payerCount); // Should still be only 1
            }
        }
    }
}
