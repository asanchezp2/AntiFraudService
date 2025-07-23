using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Infrastructure.Messaging;
using AntiFraudService.Domain.Interfaces;
using Application.Commands.UpdateTransaction;
using AntiFraudService.Appplication.Commands.UpdateTransaction;

namespace Infrastructure.Tests.Messaging
{
    public class TransactionStatusWorkerTests : IDisposable
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly Mock<IServiceScope> _serviceScopeMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<ITransactionConsumer> _transactionConsumerMock;
        private readonly Mock<UpdateTransactionHandler> _handlerMock;
        private readonly Mock<ILogger<TransactionStatusWorker>> _loggerMock;
        private readonly TransactionStatusWorker _worker;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public TransactionStatusWorkerTests()
        {
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            _serviceScopeMock = new Mock<IServiceScope>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _transactionConsumerMock = new Mock<ITransactionConsumer>();
            _handlerMock = new Mock<UpdateTransactionHandler>();
            _loggerMock = new Mock<ILogger<TransactionStatusWorker>>();
            _cancellationTokenSource = new CancellationTokenSource();

            // Setup service scope factory
            _serviceScopeFactoryMock
                .Setup(x => x.CreateScope())
                .Returns(_serviceScopeMock.Object);

            _serviceScopeMock
                .Setup(x => x.ServiceProvider)
                .Returns(_serviceProviderMock.Object);

            // Setup service provider
            _serviceProviderMock
                .Setup(x => x.GetRequiredService<ITransactionConsumer>())
                .Returns(_transactionConsumerMock.Object);

            _serviceProviderMock
                .Setup(x => x.GetRequiredService<UpdateTransactionHandler>())
                .Returns(_handlerMock.Object);

            _worker = new TransactionStatusWorker(_serviceScopeFactoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldLogStartupMessage()
        {
            // Arrange
            _cancellationTokenSource.Cancel(); // Cancel immediately to stop the loop

            // Act
            await _worker.StartAsync(_cancellationTokenSource.Token);

            // Wait a bit for the background task to start
            await Task.Delay(100);

            // Assert
            VerifyLogMessage(LogLevel.Information, "TransactionStatusWorker started");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldProcessTransactionSuccessfully()
        {
            // Arrange
            var transactionDto = new
            {
                TransactionId = Guid.NewGuid(),
                SourceAccountId = Guid.NewGuid(),
                TargetAccountId = Guid.NewGuid(),
                Value = 100.50m
            };

            var processedTransactions = 0;

            _transactionConsumerMock
                .Setup(x => x.ConsumeAsync(It.IsAny<Func<dynamic, Task>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<dynamic, Task>, CancellationToken>(async (callback, token) =>
                {
                    await callback(transactionDto);
                    processedTransactions++;
                    _cancellationTokenSource.Cancel(); // Cancel after processing one message
                });

            _handlerMock
                .Setup(x => x.Handle(It.IsAny<UpdateTransactionCommand>(), It.IsAny<CancellationToken>()))
                .Returns((Task<Application.DTOs.TransactionResponseDto>)Task.CompletedTask);

            // Act
            await _worker.StartAsync(_cancellationTokenSource.Token);
            await Task.Delay(200); // Wait for processing

            // Assert
            Assert.Equal(1, processedTransactions);

            _handlerMock.Verify(x => x.Handle(
                It.Is<UpdateTransactionCommand>(cmd =>
                    cmd.TransactionId == transactionDto.TransactionId &&
                    cmd.SourceAccountId == transactionDto.SourceAccountId &&
                    cmd.TargetAccountId == transactionDto.TargetAccountId &&
                    cmd.Value == transactionDto.Value),
                It.IsAny<CancellationToken>()), Times.Once);

            VerifyLogMessage(LogLevel.Debug, $"Successfully processed transaction {transactionDto.TransactionId}");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleTransactionProcessingException()
        {
            // Arrange
            var transactionDto = new
            {
                TransactionId = Guid.NewGuid(),
                SourceAccountId = Guid.NewGuid(),
                TargetAccountId = Guid.NewGuid(),
                Value = 100.50m
            };

            var exception = new InvalidOperationException("Processing failed");

            _transactionConsumerMock
                .Setup(x => x.ConsumeAsync(It.IsAny<Func<dynamic, Task>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<dynamic, Task>, CancellationToken>(async (callback, token) =>
                {
                    await callback(transactionDto);
                    _cancellationTokenSource.Cancel();
                });

            _handlerMock
                .Setup(x => x.Handle(It.IsAny<UpdateTransactionCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            await _worker.StartAsync(_cancellationTokenSource.Token);
            await Task.Delay(200);

            // Assert
            VerifyLogMessage(LogLevel.Error, $"Error processing transaction {transactionDto.TransactionId}");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleConsumerException_AndImplementExponentialBackoff()
        {
            // Arrange
            var exception = new InvalidOperationException("Consumer failed");
            var callCount = 0;

            _transactionConsumerMock
                .Setup(x => x.ConsumeAsync(It.IsAny<Func<dynamic, Task>>(), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    callCount++;
                    if (callCount >= 2) // Cancel after second attempt
                    {
                        _cancellationTokenSource.Cancel();
                    }
                })
                .ThrowsAsync(exception);

            // Act
            await _worker.StartAsync(_cancellationTokenSource.Token);
            await Task.Delay(6000); // Wait for backoff delay

            // Assert
            Assert.True(callCount >= 2);
            VerifyLogMessage(LogLevel.Error, "Error in TransactionStatusWorker, will retry in");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleCancellationGracefully()
        {
            // Arrange
            _transactionConsumerMock
                .Setup(x => x.ConsumeAsync(It.IsAny<Func<dynamic, Task>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            _cancellationTokenSource.Cancel();

            // Act
            await _worker.StartAsync(_cancellationTokenSource.Token);
            await Task.Delay(100);

            // Assert
            VerifyLogMessage(LogLevel.Information, "TransactionStatusWorker cancellation requested");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCloseConsumerInFinallyBlock()
        {
            // Arrange
            _cancellationTokenSource.Cancel();

            _transactionConsumerMock
                .Setup(x => x.ConsumeAsync(It.IsAny<Func<dynamic, Task>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            await _worker.StartAsync(_cancellationTokenSource.Token);
            await Task.Delay(200);

            // Assert
            _transactionConsumerMock.Verify(x => x.CloseAsync(CancellationToken.None), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldDisposeServiceScopeInFinallyBlock()
        {
            // Arrange
            _cancellationTokenSource.Cancel();

            // Act
            await _worker.StartAsync(_cancellationTokenSource.Token);
            await Task.Delay(100);

            // Assert
            _serviceScopeMock.Verify(x => x.Dispose(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleConsumerCloseException()
        {
            // Arrange
            var closeException = new InvalidOperationException("Close failed");

            _cancellationTokenSource.Cancel();

            _transactionConsumerMock
                .Setup(x => x.CloseAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(closeException);

            // Act
            await _worker.StartAsync(_cancellationTokenSource.Token);
            await Task.Delay(200);

            // Assert
            VerifyLogMessage(LogLevel.Warning, "Error closing consumer");
        }

        [Fact]
        public async Task StopAsync_ShouldLogStoppingMessage()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            await _worker.StopAsync(cancellationTokenSource.Token);

            // Assert
            VerifyLogMessage(LogLevel.Information, "TransactionStatusWorker stopping...");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldResetDelayAfterSuccessfulProcessing()
        {
            // Arrange
            var transactionDto = new
            {
                TransactionId = Guid.NewGuid(),
                SourceAccountId = Guid.NewGuid(),
                TargetAccountId = Guid.NewGuid(),
                Value = 100.50m
            };

            var callCount = 0;

            // First call throws exception, second call succeeds
            _transactionConsumerMock
                .Setup(x => x.ConsumeAsync(It.IsAny<Func<dynamic, Task>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<dynamic, Task>, CancellationToken>(async (callback, token) =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        throw new InvalidOperationException("First attempt fails");
                    }
                    else
                    {
                        await callback(transactionDto);
                        _cancellationTokenSource.Cancel();
                    }
                });

            _handlerMock
                .Setup(x => x.Handle(It.IsAny<UpdateTransactionCommand>(), It.IsAny<CancellationToken>()))
                .Returns((Task<Application.DTOs.TransactionResponseDto>)Task.CompletedTask);

            // Act
            await _worker.StartAsync(_cancellationTokenSource.Token);
            await Task.Delay(6000); // Wait for retry

            // Assert
            Assert.Equal(2, callCount);
            VerifyLogMessage(LogLevel.Debug, $"Successfully processed transaction {transactionDto.TransactionId}");
        }

        private void VerifyLogMessage(LogLevel level, string message)
        {
            _loggerMock.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _worker?.Dispose();
        }
    }
}