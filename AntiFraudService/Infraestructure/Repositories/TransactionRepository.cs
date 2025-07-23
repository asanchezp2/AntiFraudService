using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly TransactionDbContext _context;

    public TransactionRepository(TransactionDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task<Transaction?> GetByIdAsync(Guid id)
    {
        return await _context.Transactions.FindAsync(id) 
            ?? throw new KeyNotFoundException($"Transaction with ID {id} not found.");
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task<decimal> GetDailyAccumulatedAsync(Guid sourceAccountId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var result = await _context.Transactions
            .Where(t => t.SourceAccountId == sourceAccountId
                     && t.CreatedAt >= startOfDay
                     && t.CreatedAt < endOfDay
                     && t.Status == Domain.Enums.TransactionStatus.Approved)
            .SumAsync(t => (decimal?)t.Value);
        
        return result ?? 0;
    }
}