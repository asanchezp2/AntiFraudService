using Application.DTOs;
using MediatR;

namespace Application.Commands.UpdateTransaction;

public class UpdateTransactionCommand : IRequest<TransactionResponseDto>
{
    public Guid TransactionId { get; set; }
    public Guid SourceAccountId { get; set; }
    public Guid TargetAccountId { get; set; }
    public int TransferTypeId { get; set; }
    public decimal Value { get; set; }
}
