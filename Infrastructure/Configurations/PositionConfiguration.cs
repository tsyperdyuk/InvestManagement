using InvestManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestManagement.Infrastructure.Configurations;

public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(
        EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");

        builder.HasKey(position => position.Id);

        builder.HasIndex(position => new
        {
            position.PortfolioId,
            position.SecurityId
        })
        .IsUnique();

        builder.Property(position => position.Quantity)
            .HasPrecision(18, 6);

        builder.HasOne(position => position.Portfolio)
            .WithMany(portfolio => portfolio.Positions)
            .HasForeignKey(position => position.PortfolioId);

        builder.HasOne(position => position.Security)
            .WithMany(security => security.Positions)
            .HasForeignKey(position => position.SecurityId);
    }
}
