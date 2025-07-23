using AntiFraudService.Domain.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;

namespace Application.Commands.CreateTransaction;

public class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, TransactionResponseDto>
{
    private readonly ITransactionRepository _repository;
    private readonly ITransactionProducer _producer;

    public CreateTransactionHandler(ITransactionRepository repository, ITransactionProducer producer)
    {
        _repository = repository;
        _producer = producer;
    }

    public async Task<TransactionResponseDto> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionExternalId = Guid.NewGuid(),
            SourceAccountId = request.SourceAccountId,
            TargetAccountId = request.TargetAccountId,
            TransferTypeId = request.TransferTypeId,
            Value = request.Value,
            CreatedAt = DateTime.Now,
            Status = TransactionStatus.Pending
        };

        await _repository.AddAsync(transaction);

        var messageDto = new MessageDto
        {
            TransactionId = transaction.Id,
            SourceAccountId = transaction.SourceAccountId,
            TargetAccountId = transaction.TargetAccountId,
            TransferTypeId = transaction.TransferTypeId,
            Value = transaction.Value
        };

        await _producer.ProduceAsync(messageDto, cancellationToken);

        return new TransactionResponseDto
        {
            Id = transaction.Id,
            TransactionExternalId = transaction.TransactionExternalId,
            CreatedAt = transaction.CreatedAt,
            Status = transaction.Status.ToString()
        };
    }
}
