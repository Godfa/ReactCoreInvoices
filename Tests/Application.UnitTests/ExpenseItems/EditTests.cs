using Xunit;
using Application.ExpenseItems;
using System.Threading;
using System.Threading.Tasks;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Domain;
using System;
using AutoMapper;
using Moq;

namespace Application.UnitTests.ExpenseItems
{
    public class EditTests
    {
        [Fact]
        public async Task Handle_ShouldEditExpenseItem_WhenItemExists()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var expenseId = Guid.NewGuid();

            using (var context = new DataContext(options))
            {
                context.ExpenseItems.Add(new ExpenseItem { Id = expenseId, Name = "Old Name" });
                await context.SaveChangesAsync();
            }

            var mockMapper = new Mock<IMapper>();
            mockMapper.Setup(m => m.Map(It.IsAny<ExpenseItem>(), It.IsAny<ExpenseItem>()))
                      .Callback<object, object>((src, dest) => ((ExpenseItem)dest).Name = ((ExpenseItem)src).Name);

            using (var context = new DataContext(options))
            {
                var handler = new Edit.Handler(context, mockMapper.Object);
                var expenseItem = new ExpenseItem { Id = expenseId, Name = "New Name" };

                // Act
                await handler.Handle(new Edit.Command { ExpenseItem = expenseItem }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var item = await context.ExpenseItems.FindAsync(expenseId);
                Assert.NotNull(item);
                Assert.Equal("New Name", item.Name);
            }
        }
    }
}
