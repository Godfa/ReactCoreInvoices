using Xunit;
using Application.Invoices;
using System.Threading;
using System.Threading.Tasks;
using Persistence;
using Microsoft.EntityFrameworkCore;
using Domain;
using System;
using System.Linq;

namespace Application.UnitTests.Invoices
{
    public class CreateTests
    {
        [Fact]
        public async Task Handle_ShouldCreateInvoice()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                var handler = new Create.Handler(context);
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Invoice",
                    Description = "Test Description"
                };

                // Act
                await handler.Handle(new Create.Command { Invoice = invoice }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var savedInvoice = await context.Invoices.FirstOrDefaultAsync();
                Assert.NotNull(savedInvoice);
                Assert.Equal("Test Invoice", savedInvoice.Title);
                Assert.Equal("Test Description", savedInvoice.Description);
            }
        }

        [Fact]
        public async Task Handle_ShouldAutoGenerateLanNumber_WhenNoInvoicesExist()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                var handler = new Create.Handler(context);
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    Title = "First Invoice",
                    Description = "First"
                };

                // Act
                await handler.Handle(new Create.Command { Invoice = invoice }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var savedInvoice = await context.Invoices.FirstOrDefaultAsync();
                Assert.NotNull(savedInvoice);
                Assert.Equal(1, savedInvoice.LanNumber);
            }
        }

        [Fact]
        public async Task Handle_ShouldAutoGenerateLanNumber_AsMaxPlusOne()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Seed with existing invoices
            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.Invoices.AddRange(
                    new Invoice { Id = Guid.NewGuid(), Title = "Invoice 1", LanNumber = 68, Description = "Test", Status = InvoiceStatus.Arkistoitu },
                    new Invoice { Id = Guid.NewGuid(), Title = "Invoice 2", LanNumber = 69, Description = "Test", Status = InvoiceStatus.Arkistoitu }
                );
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new Create.Handler(context);
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    Title = "New Invoice",
                    Description = "New"
                };
                await handler.Handle(new Create.Command { Invoice = invoice }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var newInvoice = await context.Invoices
                    .OrderByDescending(i => i.LanNumber)
                    .FirstOrDefaultAsync();
                Assert.NotNull(newInvoice);
                Assert.Equal(70, newInvoice.LanNumber);
                Assert.Equal("New Invoice", newInvoice.Title);
            }
        }

        [Fact]
        public async Task Handle_ShouldHandleNonSequentialLanNumbers()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Seed with non-sequential LAN numbers
            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
                context.Invoices.AddRange(
                    new Invoice { Id = Guid.NewGuid(), Title = "Invoice 1", LanNumber = 10, Description = "Test", Status = InvoiceStatus.Arkistoitu },
                    new Invoice { Id = Guid.NewGuid(), Title = "Invoice 2", LanNumber = 100, Description = "Test", Status = InvoiceStatus.Arkistoitu }
                );
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new DataContext(options))
            {
                var handler = new Create.Handler(context);
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    Title = "New Invoice",
                    Description = "New"
                };
                await handler.Handle(new Create.Command { Invoice = invoice }, CancellationToken.None);
            }

            // Assert
            using (var context = new DataContext(options))
            {
                var newInvoice = await context.Invoices
                    .OrderByDescending(i => i.LanNumber)
                    .FirstOrDefaultAsync();
                Assert.NotNull(newInvoice);
                Assert.Equal(101, newInvoice.LanNumber); // Should be max (100) + 1
            }
        }
    }
}
