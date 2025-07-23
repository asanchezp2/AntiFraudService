using Application.DTOs;
using MediatR;

namespace Application.Queries.GetTransactionById;

public class GetTransactionByIdQuery : IRequest<TransactionResponseDto>
{
    public Guid TransactionId { get; }

    public GetTransactionByIdQuery(Guid transactionId)
    {
        TransactionId = transactionId;
    }
}
