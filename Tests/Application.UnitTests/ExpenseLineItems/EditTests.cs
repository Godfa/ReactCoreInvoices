using Xunit;
using Application.ExpenseLineItems;
using System.Threading;
using System.Threading.Tasks;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Domain;
using System;
using AutoMapper;
using Application.Core;

namespace Application.UnitTests.ExpenseLineItems
{
    public class EditTests
    {
        private readonly IMapper _mapper;

        public EditTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfiles>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public async Task Handle_ShouldUpdateExpenseLineItem()
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
                    Name = "Original Name",
                    Quantity = 1,
                    UnitPrice = 10.00m
                });
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new Edit.Handler(context, _mapper);
                var updatedItem = new ExpenseLineItem
                {
                    Id = lineItemId,
                    ExpenseItemId = expenseItemId,
                    Name = "Updated Name",
                    Quantity = 5,
                    UnitPrice = 20.50m
                };

                await handler.Handle(new Edit.Command { Id = lineItemId, ExpenseLineItem = updatedItem }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var item = await context.ExpenseLineItems.FindAsync(lineItemId);
                Assert.NotNull(item);
                Assert.Equal("Updated Name", item.Name);
                Assert.Equal(5, item.Quantity);
                Assert.Equal(20.50m, item.UnitPrice);
                Assert.Equal(102.50m, item.Total);
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
                var handler = new Edit.Handler(context, _mapper);
                var lineItem = new ExpenseLineItem
                {
                    Id = Guid.NewGuid(),
                    ExpenseItemId = Guid.NewGuid(),
                    Name = "Non-existent",
                    Quantity = 1,
                    UnitPrice = 5.00m
                };

                await Assert.ThrowsAsync<Exception>(async () =>
                    await handler.Handle(new Edit.Command { Id = lineItem.Id, ExpenseLineItem = lineItem }, CancellationToken.None)
                );
            }
        }
    }
}
