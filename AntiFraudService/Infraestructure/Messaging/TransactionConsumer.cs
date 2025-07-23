using AntiFraudService.Domain.Interfaces;
using Application.DTOs;
using Confluent.Kafka;
using System.Text.Json;

namespace Infrastructure.Messaging;

public class TransactionConsumer : ITransactionConsumer, IDisposable
{
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly string _topic;
    private readonly ILogger<TransactionConsumer> _logger;
    private readonly int _pollTimeoutMs;
    private bool _disposed = false;

    public TransactionConsumer(ConsumerConfig config, string topic, ILogger<TransactionConsumer> logger)
    {
        _consumer = new ConsumerBuilder<Ignore, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Reason}", e.Reason))
            .Build();

        _topic = topic;
        _logger = logger;
        _pollTimeoutMs = 1000;

        _consumer.Subscribe(_topic);
    }

    public async Task ConsumeAsync(Func<MessageDto, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && !_disposed)
            {
                try
                {
                    var cr = _consumer.Consume(TimeSpan.FromMilliseconds(_pollTimeoutMs));

                    if (cr?.Message?.Value != null && !string.IsNullOrEmpty(cr.Message.Value))
                    {
                        try
                        {
                            var message = JsonSerializer.Deserialize<MessageDto>(cr.Message.Value);
                            if (message != null)
                            {
                                await messageHandler(message);

                                _consumer.Commit(cr);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Error deserializing message from topic {Topic}, partition {Partition}, offset {Offset}. Raw message: {RawMessage}",
                                cr.Topic, cr.Partition, cr.Offset, cr.Message.Value);

                            _consumer.Commit(cr);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message from topic {Topic}, partition {Partition}, offset {Offset}",
                                cr.Topic, cr.Partition, cr.Offset);

                            _consumer.Commit(cr);
                        }
                    }
                    else if (cr == null)
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);

                    await Task.Delay(1000, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Consumer operation cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer");

                    await Task.Delay(5000, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Kafka consumer stopped due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Kafka consumer");
            throw;
        }
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _consumer?.Close();
            _disposed = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing Kafka consumer");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _consumer?.Close();
                _consumer?.Dispose();
                _disposed = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing Kafka consumer");
            }
        }
    }
}