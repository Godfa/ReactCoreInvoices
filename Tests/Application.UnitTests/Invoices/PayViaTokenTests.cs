using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Invoices;
using Domain;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Xunit;

namespace Application.UnitTests.Invoices
{
    public class PayViaTokenTests
    {
        private (DataContext context, DbContextOptions<DataContext> options) GetContext()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return (new DataContext(options), options);
        }

        [Fact]
        public async Task Handle_ShouldMarkAsPaid_WhenValidToken()
        {
            // Arrange
            var (context, options) = GetContext();
            var invoiceId = Guid.NewGuid();
            var userId = "user-1";
            var paymentToken = Guid.NewGuid();
            var participant = new InvoiceParticipant
            {
                AppUserId = userId,
                HasPaid = false,
                PaidAt = null,
                PaymentToken = paymentToken
            };

            context.Invoices.Add(new Invoice
            {
                Id = invoiceId,
                Status = InvoiceStatus.Maksussa,
                Participants = new List<InvoiceParticipant> { participant }
            });
            await context.SaveChangesAsync();

            var handler = new PayViaToken.Handler(context);
            var command = new PayViaToken.Command { Token = paymentToken };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            using (var assertContext = new DataContext(options))
            {
                var updatedParticipant = await assertContext.InvoiceParticipants
                    .FirstOrDefaultAsync(p => p.AppUserId == userId);

                Assert.NotNull(updatedParticipant);
                Assert.True(updatedParticipant.HasPaid);
                Assert.NotNull(updatedParticipant.PaidAt);
                Assert.Null(updatedParticipant.PaymentToken); // Token should be cleared
            }
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenInvalidToken()
        {
            // Arrange
            var (context, options) = GetContext();
            var invoiceId = Guid.NewGuid();
            var userId = "user-1";
            var validToken = Guid.NewGuid();
            var invalidToken = Guid.NewGuid();
            var participant = new InvoiceParticipant
            {
                AppUserId = userId,
                HasPaid = false,
                PaidAt = null,
                PaymentToken = validToken
            };

            context.Invoices.Add(new Invoice
            {
                Id = invoiceId,
                Status = InvoiceStatus.Maksussa,
                Participants = new List<InvoiceParticipant> { participant }
            });
            await context.SaveChangesAsync();

            var handler = new PayViaToken.Handler(context);
            var command = new PayViaToken.Command { Token = invalidToken };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
            Assert.Contains("Virheellinen tai vanhentunut maksulinkki", exception.Message);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenAlreadyPaid()
        {
            // Arrange
            var (context, options) = GetContext();
            var invoiceId = Guid.NewGuid();
            var userId = "user-1";
            var paymentToken = Guid.NewGuid();
            var participant = new InvoiceParticipant
            {
                AppUserId = userId,
                HasPaid = true,
                PaidAt = DateTime.UtcNow.AddDays(-1),
                PaymentToken = paymentToken
            };

            context.Invoices.Add(new Invoice
            {
                Id = invoiceId,
                Status = InvoiceStatus.Maksussa,
                Participants = new List<InvoiceParticipant> { participant }
            });
            await context.SaveChangesAsync();

            var handler = new PayViaToken.Handler(context);
            var command = new PayViaToken.Command { Token = paymentToken };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
            Assert.Contains("Tämä maksu on jo merkitty maksetuksi", exception.Message);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenInvoiceNotInMaksussaStatus()
        {
            // Arrange
            var (context, options) = GetContext();
            var invoiceId = Guid.NewGuid();
            var userId = "user-1";
            var paymentToken = Guid.NewGuid();
            var participant = new InvoiceParticipant
            {
                AppUserId = userId,
                HasPaid = false,
                PaidAt = null,
                PaymentToken = paymentToken
            };

            context.Invoices.Add(new Invoice
            {
                Id = invoiceId,
                Status = InvoiceStatus.Arkistoitu, // Not Maksussa
                Participants = new List<InvoiceParticipant> { participant }
            });
            await context.SaveChangesAsync();

            var handler = new PayViaToken.Handler(context);
            var command = new PayViaToken.Command { Token = paymentToken };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
            Assert.Contains("Lasku ei ole enää maksussa-tilassa", exception.Message);
        }

        [Fact]
        public async Task Handle_ShouldClearToken_AfterSuccessfulPayment()
        {
            // Arrange
            var (context, options) = GetContext();
            var invoiceId = Guid.NewGuid();
            var userId = "user-1";
            var paymentToken = Guid.NewGuid();
            var participant = new InvoiceParticipant
            {
                AppUserId = userId,
                HasPaid = false,
                PaidAt = null,
                PaymentToken = paymentToken
            };

            context.Invoices.Add(new Invoice
            {
                Id = invoiceId,
                Status = InvoiceStatus.Maksussa,
                Participants = new List<InvoiceParticipant> { participant }
            });
            await context.SaveChangesAsync();

            var handler = new PayViaToken.Handler(context);
            var command = new PayViaToken.Command { Token = paymentToken };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert - Try using the same token again
            var exception = await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
            Assert.Contains("Virheellinen tai vanhentunut maksulinkki", exception.Message);
        }
    }
}
