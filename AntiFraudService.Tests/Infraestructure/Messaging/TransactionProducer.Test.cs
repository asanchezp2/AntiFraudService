using System;
using System.Threading.Tasks;
using Application.DTOs;
using Confluent.Kafka;
using Infrastructure.Messaging;
using Xunit;

namespace AntiFraudService.Tests.Infrastructure.Messaging
{
    /// <summary>
    /// NOTE: These are INTEGRATION tests, not unit tests.
    /// They require a running Kafka broker accessible at the address specified
    /// in 'BootstrapServers' (e.g., localhost:9092).
    /// The TransactionProducer class, by creating its own IProducer instance,
    /// cannot be unit tested without modifying its code to inject the dependency.
    /// </summary>
    public class TransactionProducerTests
    {
        private readonly ProducerConfig _producerConfig;
        private readonly string _bootstrapServers = "localhost:9092"; // <-- CHANGE IF NECESSARY

        public TransactionProducerTests()
        {
            // Configuration for a Kafka producer.
            // This will fail if it cannot connect to the broker.
            _producerConfig = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers,
                // Use a short timeout so the test doesn't wait too long if the broker is unavailable.
                SocketTimeoutMs = 5000
            };
        }

        [Fact]
        public async Task ProduceAsync_WhenBrokerIsAvailable_ShouldProduceMessageWithoutException()
        {
            // Arrange
            var topic = $"test-topic-{Guid.NewGuid()}";
            var producer = new TransactionProducer(_producerConfig, topic);
            var messageDto = new MessageDto
            {
                TransactionId = Guid.NewGuid(),
                Value = 123.45m
            };

            // Act & Assert: The test passes if no exception is thrown when producing the message.
            // This implies that the connection to Kafka was successful and the message was sent.
            var exception = await Record.ExceptionAsync(() => producer.ProduceAsync(messageDto));
            Assert.Null(exception);
        }
    }
}
