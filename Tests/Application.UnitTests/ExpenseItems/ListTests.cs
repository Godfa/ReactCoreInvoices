using Xunit;
using Application.ExpenseItems;
using System.Threading;
using System.Threading.Tasks;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Domain;
using System.Collections.Generic;
using System;

namespace Application.UnitTests.ExpenseItems
{
    public class ListTests
    {
        [Fact]
        public async Task Handle_ShouldReturnListOfExpenseItems()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new DataContext(options))
            {
                context.ExpenseItems.Add(new ExpenseItem { Id = Guid.NewGuid(), Name = "Test Item" });
                await context.SaveChangesAsync();
            }

            using (var context = new DataContext(options))
            {
                var handler = new List.Handler(context);

                // Act
                var result = await handler.Handle(new List.Query(), CancellationToken.None);

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Equal("Test Item", result[0].Name);
            }
        }
    }
}
