using Application.Commands.UpdateTransaction;
using Application.DTOs;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;

namespace AntiFraudService.Appplication.Commands.UpdateTransaction;

public class UpdateTransactionHandler : IRequestHandler<UpdateTransactionCommand, TransactionResponseDto>
{
    private readonly ITransactionRepository _repository;

    public UpdateTransactionHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<TransactionResponseDto> Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        // Anti-fraud validation
        var status = TransactionStatus.Approved;
        var accumulated = await _repository.GetDailyAccumulatedAsync(request.SourceAccountId, DateTime.Now);
        if (request.Value > 2000 || accumulated + request.Value > 20000)
        {
            status = TransactionStatus.Rejected;
        }

        var transaction = await _repository.GetByIdAsync(request.TransactionId);
        if (transaction == null)
            throw new InvalidOperationException("Transaction not found.");

        transaction.Status = status;
        await _repository.UpdateAsync(transaction);

        return new TransactionResponseDto
        {
            TransactionExternalId = transaction.TransactionExternalId,
            CreatedAt = transaction.CreatedAt,
            Status = transaction.Status.ToString()
        };
    }
}
