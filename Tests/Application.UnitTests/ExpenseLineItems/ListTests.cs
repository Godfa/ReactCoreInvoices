using Xunit;
using Application.ExpenseLineItems;
using System.Threading;
using System.Threading.Tasks;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Domain;
using System;
using System.Linq;

namespace Application.UnitTests.ExpenseLineItems
{
    public class ListTests
    {
        [Fact]
        public async Task Handle_ShouldReturnLineItemsForExpenseItem()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseItemId = Guid.NewGuid();
            var otherExpenseItemId = Guid.NewGuid();

            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.ExpenseItems.Add(new ExpenseItem { Id = expenseItemId, Name = "Item 1" });
                context.ExpenseItems.Add(new ExpenseItem { Id = otherExpenseItemId, Name = "Item 2" });

                context.ExpenseLineItems.AddRange(
                    new ExpenseLineItem { Id = Guid.NewGuid(), ExpenseItemId = expenseItemId, Name = "Line 1", Quantity = 1, UnitPrice = 10m },
                    new ExpenseLineItem { Id = Guid.NewGuid(), ExpenseItemId = expenseItemId, Name = "Line 2", Quantity = 2, UnitPrice = 15m },
                    new ExpenseLineItem { Id = Guid.NewGuid(), ExpenseItemId = expenseItemId, Name = "Line 3", Quantity = 3, UnitPrice = 20m },
                    new ExpenseLineItem { Id = Guid.NewGuid(), ExpenseItemId = otherExpenseItemId, Name = "Other Line", Quantity = 1, UnitPrice = 5m }
                );
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new List.Handler(context);
                var result = await handler.Handle(new List.Query { ExpenseItemId = expenseItemId }, CancellationToken.None);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(3, result.Count);
                Assert.All(result, item => Assert.Equal(expenseItemId, item.ExpenseItemId));
                Assert.DoesNotContain(result, item => item.Name == "Other Line");
            }
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoLineItems()
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

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new List.Handler(context);
                var result = await handler.Handle(new List.Query { ExpenseItemId = expenseItemId }, CancellationToken.None);

                // Assert
                Assert.NotNull(result);
                Assert.Empty(result);
            }
        }

        [Fact]
        public async Task Handle_ShouldReturnLineItemsOrderedByName()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseItemId = Guid.NewGuid();

            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.ExpenseItems.Add(new ExpenseItem { Id = expenseItemId, Name = "Parent" });

                context.ExpenseLineItems.AddRange(
                    new ExpenseLineItem { Id = Guid.NewGuid(), ExpenseItemId = expenseItemId, Name = "Zebra", Quantity = 1, UnitPrice = 10m },
                    new ExpenseLineItem { Id = Guid.NewGuid(), ExpenseItemId = expenseItemId, Name = "Apple", Quantity = 1, UnitPrice = 10m },
                    new ExpenseLineItem { Id = Guid.NewGuid(), ExpenseItemId = expenseItemId, Name = "Mango", Quantity = 1, UnitPrice = 10m }
                );
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new List.Handler(context);
                var result = await handler.Handle(new List.Query { ExpenseItemId = expenseItemId }, CancellationToken.None);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(3, result.Count);
                Assert.Equal("Apple", result[0].Name);
                Assert.Equal("Mango", result[1].Name);
                Assert.Equal("Zebra", result[2].Name);
            }
        }
    }
}
