using Application.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.Queries.GetTransactionById;

public class GetTransactionByIdHandler : IRequestHandler<GetTransactionByIdQuery, TransactionResponseDto?>
{
    private readonly ITransactionRepository _repository;

    public GetTransactionByIdHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<TransactionResponseDto?> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByIdAsync(request.TransactionId);

        if (transaction == null)
            return null;

        return new TransactionResponseDto
        {
            TransactionExternalId = transaction.TransactionExternalId,
            CreatedAt = transaction.CreatedAt,
            Status = transaction.Status.ToString().ToLower()
        };
    }
}
