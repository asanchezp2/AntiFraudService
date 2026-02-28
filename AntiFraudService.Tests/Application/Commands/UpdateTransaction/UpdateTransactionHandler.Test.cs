using AntiFraudService.Application.Commands.UpdateTransaction;
using Application.Commands.UpdateTransaction;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Moq;

namespace AntiFraudService.Tests.Application.Commands.UpdateTransaction
{
    public class UpdateTransactionHandlerTests
    {
        private readonly Mock<ITransactionRepository> _repositoryMock;
        private readonly UpdateTransactionHandler _handler;

        public UpdateTransactionHandlerTests()
        {
            _repositoryMock = new Mock<ITransactionRepository>();
            _handler = new UpdateTransactionHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldApproveTransaction_WhenValueAndAccumulatedAreWithinLimits()
        {
            // Arrange
            var command = new UpdateTransactionCommand
            {
                TransactionId = Guid.NewGuid(),
                SourceAccountId = Guid.NewGuid(),
                Value = 1500m
            };

            var transaction = new Transaction { Id = command.TransactionId, TransactionExternalId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };

            _repositoryMock.Setup(r => r.GetDailyAccumulatedAsync(command.SourceAccountId, It.IsAny<DateTime>()))
                           .ReturnsAsync(10000m);

            _repositoryMock.Setup(r => r.GetByIdAsync(command.TransactionId))
                           .ReturnsAsync(transaction);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Transaction>(t => t.Status == TransactionStatus.Approved)), Times.Once);
            Assert.NotNull(result);
            Assert.Equal(TransactionStatus.Approved.ToString(), result.Status);
            Assert.Equal(transaction.TransactionExternalId, result.TransactionExternalId);
        }

        [Fact]
        public async Task Handle_ShouldRejectTransaction_WhenValueIsOverLimit()
        {
            // Arrange
            var command = new UpdateTransactionCommand
            {
                TransactionId = Guid.NewGuid(),
                SourceAccountId = Guid.NewGuid(),
                Value = 2001m // Value over the 2000 limit
            };

            var transaction = new Transaction { Id = command.TransactionId, TransactionExternalId = Guid.NewGuid() };

            _repositoryMock.Setup(r => r.GetDailyAccumulatedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>())).ReturnsAsync(0m);
            _repositoryMock.Setup(r => r.GetByIdAsync(command.TransactionId)).ReturnsAsync(transaction);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Transaction>(t => t.Status == TransactionStatus.Rejected)), Times.Once);
            Assert.Equal(TransactionStatus.Rejected.ToString(), result.Status);
        }

        [Fact]
        public async Task Handle_ShouldRejectTransaction_WhenAccumulatedIsOverLimit()
        {
            // Arrange
            var command = new UpdateTransactionCommand
            {
                TransactionId = Guid.NewGuid(),
                SourceAccountId = Guid.NewGuid(),
                Value = 500m
            };

            var transaction = new Transaction { Id = command.TransactionId, TransactionExternalId = Guid.NewGuid() };

            // Accumulated + Value will be over 20000
            _repositoryMock.Setup(r => r.GetDailyAccumulatedAsync(command.SourceAccountId, It.IsAny<DateTime>()))
                           .ReturnsAsync(19600m);

            _repositoryMock.Setup(r => r.GetByIdAsync(command.TransactionId)).ReturnsAsync(transaction);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Transaction>(t => t.Status == TransactionStatus.Rejected)), Times.Once);
            Assert.Equal(TransactionStatus.Rejected.ToString(), result.Status);
        }

        [Fact]
        public async Task Handle_ShouldThrowInvalidOperationException_WhenTransactionNotFound()
        {
            // Arrange
            var command = new UpdateTransactionCommand
            {
                TransactionId = Guid.NewGuid(),
                SourceAccountId = Guid.NewGuid(),
                Value = 100m
            };

            _repositoryMock.Setup(r => r.GetDailyAccumulatedAsync(It.IsAny<Guid>(), It.IsAny<DateTime>())).ReturnsAsync(0m);
            _repositoryMock.Setup(r => r.GetByIdAsync(command.TransactionId)).ReturnsAsync((Transaction)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
        }
    }
}

