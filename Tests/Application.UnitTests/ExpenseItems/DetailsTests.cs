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
    public class DetailsTests
    {
        [Fact]
        public async Task Handle_ShouldReturnExpenseItem_WhenItemExists()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseId = Guid.NewGuid();

            using (var context = new DataContext(options))
            {
                context.ExpenseItems.Add(new ExpenseItem { Id = expenseId, Name = "Detail Item" });
                await context.SaveChangesAsync();
            }

            using (var context = new DataContext(options))
            {
                var handler = new Details.Handler(context);

                // Act
                var result = await handler.Handle(new Details.Query { Id = expenseId }, CancellationToken.None);

                // Assert
                Assert.NotNull(result);
                Assert.Equal("Detail Item", result.Name);
            }
        }
    }
}
