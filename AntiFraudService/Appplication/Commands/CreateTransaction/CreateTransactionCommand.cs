using Application.DTOs;
using MediatR;

namespace Application.Commands.CreateTransaction;

public class CreateTransactionCommand : IRequest<TransactionResponseDto>
{
    public Guid SourceAccountId { get; set; }
    public Guid TargetAccountId { get; set; }
    public int TransferTypeId { get; set; }
    public decimal Value { get; set; }
}

