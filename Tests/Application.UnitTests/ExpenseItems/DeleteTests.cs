using Xunit;
using Application.ExpenseItems;
using System.Threading;
using System.Threading.Tasks;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Domain;
using System;

namespace Application.UnitTests.ExpenseItems
{
    public class DeleteTests
    {
        [Fact]
        public async Task Handle_ShouldDeleteExpenseItem_WhenItemExists()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseId = Guid.NewGuid();

            using (var context = new DataContext(options))
            {
                context.ExpenseItems.Add(new ExpenseItem { Id = expenseId, Name = "To Delete" });
                await context.SaveChangesAsync();
            }

            using (var context = new DataContext(options))
            {
                var handler = new Delete.Handler(context);

                // Act
                await handler.Handle(new Delete.Command { Id = expenseId }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var item = await context.ExpenseItems.FindAsync(expenseId);
                Assert.Null(item);
            }
        }
    }
}
