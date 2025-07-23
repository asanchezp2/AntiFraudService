using Domain.Entities;

namespace Domain.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction> GetByIdAsync(Guid id);
    Task AddAsync(Transaction transaction);
    Task<decimal> GetDailyAccumulatedAsync(Guid sourceAccountId, DateTime date);
    Task UpdateAsync(Transaction transaction);
}
