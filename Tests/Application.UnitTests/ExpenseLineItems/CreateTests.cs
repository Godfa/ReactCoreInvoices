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
    public class CreateTests
    {
        [Fact]
        public async Task Handle_ShouldCreateExpenseLineItem()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseItemId = Guid.NewGuid();

            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.ExpenseItems.Add(new ExpenseItem { Id = expenseItemId, Name = "Parent Item" });
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new Create.Handler(context);
                var lineItem = new ExpenseLineItem
                {
                    Id = Guid.NewGuid(),
                    ExpenseItemId = expenseItemId,
                    Name = "Line Item 1",
                    Quantity = 2,
                    UnitPrice = 10.50m
                };

                await handler.Handle(new Create.Command { ExpenseLineItem = lineItem }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var item = await context.ExpenseLineItems.FirstOrDefaultAsync();
                Assert.NotNull(item);
                Assert.Equal("Line Item 1", item.Name);
                Assert.Equal(2, item.Quantity);
                Assert.Equal(10.50m, item.UnitPrice);
                Assert.Equal(21.00m, item.Total);
            }
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenExpenseItemNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
            }

            // Act & Assert
            using (var context = new DataContext(options))
            {
                var handler = new Create.Handler(context);
                var lineItem = new ExpenseLineItem
                {
                    Id = Guid.NewGuid(),
                    ExpenseItemId = Guid.NewGuid(),
                    Name = "Orphan Item",
                    Quantity = 1,
                    UnitPrice = 5.00m
                };

                await Assert.ThrowsAsync<Exception>(async () =>
                    await handler.Handle(new Create.Command { ExpenseLineItem = lineItem }, CancellationToken.None)
                );
            }
        }

        [Fact]
        public async Task Handle_ShouldCalculateTotal_Correctly()
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
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new Create.Handler(context);
                var lineItem = new ExpenseLineItem
                {
                    Id = Guid.NewGuid(),
                    ExpenseItemId = expenseItemId,
                    Name = "Item with Decimal",
                    Quantity = 3,
                    UnitPrice = 15.99m
                };

                await handler.Handle(new Create.Command { ExpenseLineItem = lineItem }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var item = await context.ExpenseLineItems.FirstOrDefaultAsync();
                Assert.NotNull(item);
                Assert.Equal(47.97m, item.Total); // 3 * 15.99
            }
        }
    }
}
