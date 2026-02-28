using AntiFraudService.Domain.Interfaces;
using Application.DTOs;
using Confluent.Kafka;
using System.Text.Json;

namespace Infrastructure.Messaging;

public class TransactionProducer : ITransactionProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;

    public TransactionProducer(ProducerConfig config, string topic)
    {
        _producer = new ProducerBuilder<string, string>(config)
            .SetKeySerializer(Serializers.Utf8)
            .SetValueSerializer(Serializers.Utf8)
            .Build();

        _topic = topic;
    }

    public async Task ProduceAsync(MessageDto messageDto, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(messageDto);
        var message = new Message<string, string>
        {
            Key = messageDto.TransactionId.ToString(),
            Value = json
        };

        await _producer.ProduceAsync(_topic, message, cancellationToken);
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _producer.Flush(cancellationToken);
        _producer.Dispose();
        return Task.CompletedTask;
    }
}

