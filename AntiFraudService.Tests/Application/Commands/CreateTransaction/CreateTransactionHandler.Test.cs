using AntiFraudService.Domain.Interfaces;
using Application.Commands.CreateTransaction;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Moq;

namespace AntiFraudService.Tests.Application.Commands
{
    public class CreateTransactionHandlerTests
    {
        private readonly Mock<ITransactionRepository> _repositoryMock;
        private readonly Mock<ITransactionProducer> _producerMock;
        private readonly CreateTransactionHandler _handler;

        public CreateTransactionHandlerTests()
        {
            _repositoryMock = new Mock<ITransactionRepository>();
            _producerMock = new Mock<ITransactionProducer>();
            _handler = new CreateTransactionHandler(_repositoryMock.Object, _producerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldCreateTransactionAndProduceMessage_WhenCalled()
        {
            // Arrange
            var command = new CreateTransactionCommand
            {
                SourceAccountId = Guid.NewGuid(),
                TargetAccountId = Guid.NewGuid(),
                TransferTypeId = 1,
                Value = 150
            };

            Transaction? capturedTransaction = null;

            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => capturedTransaction = t)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(capturedTransaction);

            _repositoryMock.Verify(r => r.AddAsync(capturedTransaction), Times.Once);

            _producerMock.Verify(
                p => p.ProduceAsync(
                    It.Is<MessageDto>(m =>
                        m.TransactionId == capturedTransaction.Id &&
                        m.Value == command.Value),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.NotNull(result);
            Assert.Equal(capturedTransaction.Id, result.Id);
            Assert.Equal(capturedTransaction.TransactionExternalId, result.TransactionExternalId);
            Assert.Equal(TransactionStatus.Pending.ToString(), result.Status);
            Assert.Equal(command.Value, capturedTransaction.Value);
        }
    }
}
