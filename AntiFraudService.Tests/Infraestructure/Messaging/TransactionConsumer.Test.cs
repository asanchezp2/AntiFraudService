using Application.DTOs;
using Confluent.Kafka;
using Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace AntiFraudService.Tests.Infrastructure.Messaging
{
    public class TransactionConsumerTests
    {
        private readonly Mock<IConsumer<Ignore, string>> _kafkaConsumerMock;
        private readonly Mock<ILogger<TransactionConsumer>> _loggerMock;
        private readonly TransactionConsumer _transactionConsumer;

        public TransactionConsumerTests()
        {
            _kafkaConsumerMock = new Mock<IConsumer<Ignore, string>>();
            _loggerMock = new Mock<ILogger<TransactionConsumer>>();

            // The consumer configuration is irrelevant for these tests,
            // since we are mocking the IConsumer interface.
            var consumerConfig = new ConsumerConfig();
            var topic = "test-topic";

            _transactionConsumer = new TransactionConsumer(consumerConfig, topic, _loggerMock.Object);

            // We replace the real internal instance with our mock.
            // This is possible using reflection or, more cleanly, by modifying the constructor
            // to allow mock injection (which is a better practice).
            // For simplicity, we'll configure the mock directly using reflection here.
            // An alternative is to have an `internal` constructor for testing purposes.
            var consumerField = typeof(TransactionConsumer).GetField("_consumer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            consumerField.SetValue(_transactionConsumer, _kafkaConsumerMock.Object);
        }

        private ConsumeResult<Ignore, string> CreateConsumeResult(string value)
        {
            return new ConsumeResult<Ignore, string>
            {
                Message = new Message<Ignore, string> { Value = value },
                TopicPartitionOffset = new TopicPartitionOffset("test-topic", 0, 0)
            };
        }

        [Fact]
        public async Task ConsumeAsync_ShouldProcessValidMessage_AndCommit()
        {
            // Arrange
            var messageDto = new MessageDto { TransactionId = Guid.NewGuid(), Value = 100 };
            var jsonMessage = JsonSerializer.Serialize(messageDto);
            var consumeResult = CreateConsumeResult(jsonMessage);
            var cts = new CancellationTokenSource();

            _kafkaConsumerMock.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                .Returns(consumeResult)
                .Callback(() => cts.Cancel()); // Cancel after the first read to end the loop

            MessageDto capturedDto = null;
            Func<MessageDto, Task> handler = (dto) =>
            {
                capturedDto = dto;
                return Task.CompletedTask;
            };

            // Act
            await _transactionConsumer.ConsumeAsync(handler, cts.Token);

            // Assert
            Assert.NotNull(capturedDto);
            Assert.Equal(messageDto.TransactionId, capturedDto.TransactionId);
            _kafkaConsumerMock.Verify(c => c.Commit(consumeResult), Times.Once);
        }

        [Fact]
        public async Task ConsumeAsync_ShouldLogAndCommit_WhenJsonIsInvalid()
        {
            // Arrange
            var invalidJson = "{ not_a_valid_json }";
            var consumeResult = CreateConsumeResult(invalidJson);
            var cts = new CancellationTokenSource();

            _kafkaConsumerMock.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                .Returns(consumeResult)
                .Callback(() => cts.Cancel());

            var handlerCalled = false;
            Func<MessageDto, Task> handler = (dto) =>
            {
                handlerCalled = true;
                return Task.CompletedTask;
            };

            // Act
            await _transactionConsumer.ConsumeAsync(handler, cts.Token);

            // Assert
            Assert.False(handlerCalled);
            _kafkaConsumerMock.Verify(c => c.Commit(consumeResult), Times.Once, "Should commit to skip the poison pill message.");
            _loggerMock.Verify(
                x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error deserializing message")), It.IsAny<JsonException>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ConsumeAsync_ShouldLogAndCommit_WhenHandlerThrowsException()
        {
            // Arrange
            var messageDto = new MessageDto { TransactionId = Guid.NewGuid(), Value = 100 };
            var jsonMessage = JsonSerializer.Serialize(messageDto);
            var consumeResult = CreateConsumeResult(jsonMessage);
            var cts = new CancellationTokenSource();
            var handlerException = new InvalidOperationException("Handler failed");

            _kafkaConsumerMock.Setup(c => c.Consume(It.IsAny<TimeSpan>()))
                .Returns(consumeResult)
                .Callback(() => cts.Cancel());

            Func<MessageDto, Task> handler = (dto) => throw handlerException;

            // Act
            await _transactionConsumer.ConsumeAsync(handler, cts.Token);

            // Assert
            _kafkaConsumerMock.Verify(c => c.Commit(consumeResult), Times.Once, "Should commit even if handler fails.");
            _loggerMock.Verify(
                x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error processing message")), It.Is<InvalidOperationException>(ex => ex == handlerException), It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
