using Application.DTOs;

namespace AntiFraudService.Domain.Interfaces;

public interface ITransactionProducer
{
    Task ProduceAsync(MessageDto messageDto, CancellationToken cancellationToken = default);

    Task CloseAsync(CancellationToken cancellationToken = default);
}
