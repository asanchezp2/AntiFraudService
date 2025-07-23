namespace Application.DTOs;

public class CreateTransactionDto
{
    public Guid SourceAccountId { get; set; }
    public decimal Value { get; set; }
}
