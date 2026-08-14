using InvestManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestManagement.Infrastructure.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 6);

        builder.Property(x => x.Price)
            .HasPrecision(18, 2);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Portfolio)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Security)
            .WithMany()
            .HasForeignKey(x => x.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PortfolioId);

        builder.HasIndex(x => x.SecurityId);
    }
}