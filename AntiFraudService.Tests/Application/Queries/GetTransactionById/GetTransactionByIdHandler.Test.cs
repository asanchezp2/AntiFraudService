using Application.Queries.GetTransactionById;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Moq;

namespace AntiFraudService.Tests.Application.Queries
{
    public class GetTransactionByIdHandlerTests
    {
        private readonly Mock<ITransactionRepository> _repositoryMock;
        private readonly GetTransactionByIdHandler _handler;

        public GetTransactionByIdHandlerTests()
        {
            _repositoryMock = new Mock<ITransactionRepository>();
            _handler = new GetTransactionByIdHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnTransactionResponseDto_WhenTransactionExists()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transaction = new Transaction
            {
                Id = transactionId,
                TransactionExternalId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Status = TransactionStatus.Approved
            };

            _repositoryMock
                .Setup(r => r.GetByIdAsync(transactionId))
                .ReturnsAsync(transaction);

            var query = new GetTransactionByIdQuery(transactionId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(transaction.TransactionExternalId, result.TransactionExternalId);
            Assert.Equal(transaction.CreatedAt, result.CreatedAt);
            Assert.Equal(transaction.Status.ToString().ToLower(), result.Status);
            _repositoryMock.Verify(r => r.GetByIdAsync(transactionId), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnNull_WhenTransactionDoesNotExist()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(transactionId)).ReturnsAsync((Transaction?)null);
            var query = new GetTransactionByIdQuery(transactionId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
            _repositoryMock.Verify(r => r.GetByIdAsync(transactionId), Times.Once);
        }
    }
}

