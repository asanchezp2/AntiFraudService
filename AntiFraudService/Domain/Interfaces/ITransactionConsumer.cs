using Application.DTOs;

namespace AntiFraudService.Domain.Interfaces;

public interface ITransactionConsumer
{
    Task ConsumeAsync(Func<MessageDto, Task> messageHandler, CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
}
