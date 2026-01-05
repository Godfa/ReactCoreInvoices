using Xunit;
using System.Threading.Tasks;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Domain;
using System;
using System.Linq;

namespace Application.UnitTests.ExpenseLineItems
{
    public class CascadeDeleteTests
    {
        [Fact]
        public async Task DeletingExpenseItem_ShouldHaveCascadeDeleteConfigured()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseItemId = Guid.NewGuid();
            var lineItemId1 = Guid.NewGuid();
            var lineItemId2 = Guid.NewGuid();

            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.ExpenseItems.Add(new ExpenseItem { Id = expenseItemId, Name = "Parent" });
                context.ExpenseLineItems.AddRange(
                    new ExpenseLineItem { Id = lineItemId1, ExpenseItemId = expenseItemId, Name = "Child 1", Quantity = 1, UnitPrice = 10m },
                    new ExpenseLineItem { Id = lineItemId2, ExpenseItemId = expenseItemId, Name = "Child 2", Quantity = 2, UnitPrice = 15m }
                );
                await context.SaveChangesAsync();
            }

            // Act - Verify cascade delete is configured
            using (var context = new DataContext(options))
            {
                var entityType = context.Model.FindEntityType(typeof(ExpenseLineItem));
                var foreignKey = entityType.GetForeignKeys()
                    .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(ExpenseItem));

                // Assert - Cascade delete should be configured
                Assert.NotNull(foreignKey);
                Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
            }
        }

        [Fact]
        public async Task ExpenseItemAmount_ShouldBeComputedFromLineItems()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseItemId = Guid.NewGuid();

            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                var expenseItem = new ExpenseItem { Id = expenseItemId, Name = "Shopping" };
                context.ExpenseItems.Add(expenseItem);
                await context.SaveChangesAsync();

                // Add line items
                context.ExpenseLineItems.AddRange(
                    new ExpenseLineItem { Id = Guid.NewGuid(), ExpenseItemId = expenseItemId, Name = "Item 1", Quantity = 2, UnitPrice = 10.50m }, // 21.00
                    new ExpenseLineItem { Id = Guid.NewGuid(), ExpenseItemId = expenseItemId, Name = "Item 2", Quantity = 3, UnitPrice = 5.99m },   // 17.97
                    new ExpenseLineItem { Id = Guid.NewGuid(), ExpenseItemId = expenseItemId, Name = "Item 3", Quantity = 1, UnitPrice = 15.00m }  // 15.00
                );
                await context.SaveChangesAsync();
            }

            // Act & Assert
            using (var context = new DataContext(options))
            {
                var expenseItem = await context.ExpenseItems
                    .Include(ei => ei.LineItems)
                    .FirstOrDefaultAsync(ei => ei.Id == expenseItemId);

                Assert.NotNull(expenseItem);
                Assert.Equal(3, expenseItem.LineItems.Count);
                Assert.Equal(53.97m, expenseItem.Amount); // 21.00 + 17.97 + 15.00
            }
        }

        [Fact]
        public async Task ExpenseItemAmount_ShouldBeZero_WhenNoLineItems()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseItemId = Guid.NewGuid();

            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.ExpenseItems.Add(new ExpenseItem { Id = expenseItemId, Name = "Empty Item" });
                await context.SaveChangesAsync();
            }

            // Act & Assert
            using (var context = new DataContext(options))
            {
                var expenseItem = await context.ExpenseItems
                    .Include(ei => ei.LineItems)
                    .FirstOrDefaultAsync(ei => ei.Id == expenseItemId);

                Assert.NotNull(expenseItem);
                Assert.Empty(expenseItem.LineItems);
                Assert.Equal(0m, expenseItem.Amount);
            }
        }

        [Fact]
        public async Task ExpenseItemAmount_ShouldUpdate_WhenLineItemsChange()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseItemId = Guid.NewGuid();
            var lineItemId = Guid.NewGuid();

            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.ExpenseItems.Add(new ExpenseItem { Id = expenseItemId, Name = "Item" });
                context.ExpenseLineItems.Add(new ExpenseLineItem
                {
                    Id = lineItemId,
                    ExpenseItemId = expenseItemId,
                    Name = "Line",
                    Quantity = 1,
                    UnitPrice = 10.00m
                });
                await context.SaveChangesAsync();
            }

            // Verify initial amount
            using (var context = new DataContext(options))
            {
                var expenseItem = await context.ExpenseItems
                    .Include(ei => ei.LineItems)
                    .FirstOrDefaultAsync(ei => ei.Id == expenseItemId);
                Assert.Equal(10.00m, expenseItem.Amount);
            }

            // Act - Update line item
            using (var context = new DataContext(options))
            {
                var lineItem = await context.ExpenseLineItems.FindAsync(lineItemId);
                lineItem.Quantity = 5;
                lineItem.UnitPrice = 20.50m;
                await context.SaveChangesAsync();
            }

            // Assert - Amount should reflect updated line item
            using (var context = new DataContext(options))
            {
                var expenseItem = await context.ExpenseItems
                    .Include(ei => ei.LineItems)
                    .FirstOrDefaultAsync(ei => ei.Id == expenseItemId);
                Assert.Equal(102.50m, expenseItem.Amount); // 5 * 20.50
            }
        }
    }
}
