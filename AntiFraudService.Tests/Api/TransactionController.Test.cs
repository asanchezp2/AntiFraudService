using API.Controllers;
using Application.Commands.CreateTransaction;
using Application.DTOs;
using Application.Queries.GetTransactionById;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AntiFraudService.Tests.Api.Controllers
{
    public class TransactionControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly TransactionController _controller;

        public TransactionControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new TransactionController(_mediatorMock.Object);
        }

        [Fact]
        public async Task CreateTransaction_ShouldReturnCreatedAtAction_WhenCommandIsValid()
        {
            // Arrange
            var command = new CreateTransactionCommand
            {
                SourceAccountId = Guid.NewGuid(),
                TargetAccountId = Guid.NewGuid(),
                TransferTypeId = 1,
                Value = 100
            };

            var transactionResponse = new TransactionResponseDto
            {
                TransactionExternalId = Guid.NewGuid(),
                CreatedAt = DateTime.Now,
                Status = "pending"
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateTransactionCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionResponse);

            // Act
            var result = await _controller.CreateTransaction(command);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdAtActionResult.StatusCode);
            Assert.Equal(nameof(_controller.GetTransactionById), createdAtActionResult.ActionName);
            Assert.Equal(transactionResponse, createdAtActionResult.Value);
        }

        [Fact]
        public async Task GetTransactionById_ShouldReturnOk_WhenTransactionExists()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transactionResponse = new TransactionResponseDto
            {
                TransactionExternalId = transactionId,
                CreatedAt = DateTime.UtcNow,
                Status = "approved"
            };

            _mediatorMock
                .Setup(m => m.Send(It.Is<GetTransactionByIdQuery>(q => q.TransactionId == transactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionResponse);

            // Act
            var result = await _controller.GetTransactionById(transactionId);

            // Assert
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okObjectResult.StatusCode);
            Assert.Equal(transactionResponse, okObjectResult.Value);
        }

        [Fact]
        public async Task GetTransactionById_ShouldReturnNotFound_WhenTransactionDoesNotExist()
        {
            // Arrange
            var transactionId = Guid.NewGuid();

            // Configuramos MediatR para que devuelva null para esta consulta específica.
            // Es importante castear `null` al tipo esperado por ReturnsAsync.
            _mediatorMock
                .Setup(m => m.Send(It.Is<GetTransactionByIdQuery>(q => q.TransactionId == transactionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TransactionResponseDto?)null);

            // Act
            var result = await _controller.GetTransactionById(transactionId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }
    }
}