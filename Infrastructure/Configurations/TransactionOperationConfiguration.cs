using InvestManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestManagement.Infrastructure.Configurations;

public sealed class TransactionOperationConfiguration
    : IEntityTypeConfiguration<TransactionOperation>
{
    public void Configure(
        EntityTypeBuilder<TransactionOperation> builder)
    {
        builder.ToTable("TransactionOperations");

        builder.HasKey(operation => operation.Id);

        builder.Property(operation => operation.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(operation => operation.Type)
            .IsRequired();

        builder.Property(operation => operation.Status)
            .IsRequired()
            .IsConcurrencyToken();

        builder.Property(operation => operation.CreatedAtUtc)
            .IsRequired();

        builder.Property(operation => operation.Quantity)
            .HasPrecision(18, 6);

        builder.HasOne<Security>()
            .WithMany()
            .HasForeignKey(operation => operation.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Portfolio>()
            .WithMany()
            .HasForeignKey(operation => operation.PortfolioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(operation => operation.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(operation => operation.TransactionId)
            .IsUnique();

        builder.HasIndex(operation => new
        {
            operation.UserId,
            operation.CreatedAtUtc
        });
    }
}
