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
    public class DeleteTests
    {
        [Fact]
        public async Task Handle_ShouldDeleteExpenseLineItem()
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
                context.ExpenseItems.Add(new ExpenseItem { Id = expenseItemId, Name = "Parent" });
                context.ExpenseLineItems.Add(new ExpenseLineItem
                {
                    Id = lineItemId,
                    ExpenseItemId = expenseItemId,
                    Name = "Item to Delete",
                    Quantity = 1,
                    UnitPrice = 10.00m
                });
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new Delete.Handler(context);
                await handler.Handle(new Delete.Command { Id = lineItemId }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var item = await context.ExpenseLineItems.FindAsync(lineItemId);
                Assert.Null(item);
            }
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenLineItemNotFound()
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
                var handler = new Delete.Handler(context);
                var nonExistentId = Guid.NewGuid();

                await Assert.ThrowsAsync<Exception>(async () =>
                    await handler.Handle(new Delete.Command { Id = nonExistentId }, CancellationToken.None)
                );
            }
        }

        [Fact]
        public async Task Handle_ShouldNotDeleteParentExpenseItem()
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
                context.ExpenseItems.Add(new ExpenseItem { Id = expenseItemId, Name = "Parent" });
                context.ExpenseLineItems.Add(new ExpenseLineItem
                {
                    Id = lineItemId,
                    ExpenseItemId = expenseItemId,
                    Name = "Child Item",
                    Quantity = 1,
                    UnitPrice = 10.00m
                });
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new Delete.Handler(context);
                await handler.Handle(new Delete.Command { Id = lineItemId }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var parentItem = await context.ExpenseItems.FindAsync(expenseItemId);
                Assert.NotNull(parentItem);
                Assert.Equal("Parent", parentItem.Name);
            }
        }
    }
}
