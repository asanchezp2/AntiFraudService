using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;
public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(builder =>
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasColumnType("uniqueidentifier");

            builder.Property(t => t.TransactionExternalId)
                .HasColumnType("uniqueidentifier");

            builder.Property(t => t.SourceAccountId)
                .HasColumnType("uniqueidentifier");

            builder.Property(t => t.TargetAccountId)
                .HasColumnType("uniqueidentifier");

            builder.Property(t => t.Status)
                .HasConversion<string>();

            builder.Property(t => t.Value)
                .HasColumnType("decimal(18,2)");
        });
    }
}