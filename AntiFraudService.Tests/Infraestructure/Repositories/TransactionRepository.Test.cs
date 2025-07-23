using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AntiFraudService.Tests.Infrastructure.Repositories
{
    public class TransactionRepositoryTests : IDisposable
    {
        private readonly TransactionDbContext _context;
        private readonly TransactionRepository _repository;

        public TransactionRepositoryTests()
        {
            // Use a unique in-memory database for each test class instance to ensure isolation.
            var options = new DbContextOptionsBuilder<TransactionDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TransactionDbContext(options);
            _repository = new TransactionRepository(_context);
        }

        [Fact]
        public async Task AddAsync_ShouldAddTransactionToDatabase()
        {
            // Arrange
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                SourceAccountId = Guid.NewGuid(),
                TargetAccountId = Guid.NewGuid(),
                Value = 100,
                Status = TransactionStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            await _repository.AddAsync(transaction);

            // Assert
            var addedTransaction = await _context.Transactions.FindAsync(transaction.Id);
            Assert.NotNull(addedTransaction);
            Assert.Equal(transaction.Id, addedTransaction.Id);
            Assert.Equal(100, addedTransaction.Value);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnTransaction_WhenItExists()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transaction = new Transaction { Id = transactionId, Value = 200 };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(transactionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(transactionId, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowKeyNotFoundException_WhenTransactionDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _repository.GetByIdAsync(nonExistentId));
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyTransactionInDatabase()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transaction = new Transaction { Id = transactionId, Status = TransactionStatus.Pending, Value = 300 };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Detach the entity to simulate a real-world scenario where the entity is modified
            // in a different context and then passed to the Update method.
            _context.Entry(transaction).State = EntityState.Detached;

            var updatedTransaction = new Transaction { Id = transactionId, Status = TransactionStatus.Approved, Value = 300 };

            // Act
            await _repository.UpdateAsync(updatedTransaction);

            // Assert
            var result = await _context.Transactions.FindAsync(transactionId);
            Assert.NotNull(result);
            Assert.Equal(TransactionStatus.Approved, result.Status);
        }

        [Fact]
        public async Task GetDailyAccumulatedAsync_ShouldReturnCorrectSum_ForApprovedTransactionsOnSpecificDate()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            var transactions = new List<Transaction>
            {
                // Approved transactions for today (should be included in sum)
                new Transaction { SourceAccountId = accountId, Value = 100, CreatedAt = today.AddHours(1), Status = TransactionStatus.Approved },
                new Transaction { SourceAccountId = accountId, Value = 50, CreatedAt = today.AddHours(2), Status = TransactionStatus.Approved },

                // Pending transaction for today (should be ignored)
                new Transaction { SourceAccountId = accountId, Value = 200, CreatedAt = today.AddHours(3), Status = TransactionStatus.Pending },

                // Approved transaction for yesterday (should be ignored)
                new Transaction { SourceAccountId = accountId, Value = 300, CreatedAt = today.AddDays(-1), Status = TransactionStatus.Approved },

                // Approved transaction for another account (should be ignored)
                new Transaction { SourceAccountId = Guid.NewGuid(), Value = 400, CreatedAt = today.AddHours(4), Status = TransactionStatus.Approved }
            };

            _context.Transactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetDailyAccumulatedAsync(accountId, today);

            // Assert
            Assert.Equal(150, result);
        }

        [Fact]
        public async Task GetDailyAccumulatedAsync_ShouldReturnZero_WhenNoApprovedTransactionsExist()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            // Act
            var result = await _repository.GetDailyAccumulatedAsync(accountId, today);

            // Assert
            Assert.Equal(0, result);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}

