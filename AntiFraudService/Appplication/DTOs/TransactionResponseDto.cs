namespace Application.DTOs;

public class TransactionResponseDto
{
    public Guid Id { get; set; }
    public Guid TransactionExternalId { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string Status { get; set; }
}
