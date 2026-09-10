using InvestManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestManagement.Infrastructure.Configurations;

public sealed class ScenarioOperationConfiguration
    : IEntityTypeConfiguration<ScenarioOperation>
{
    public void Configure(EntityTypeBuilder<ScenarioOperation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 6);

        builder.Property(x => x.Order)
            .IsRequired();

        builder.HasOne(x => x.Security)
            .WithMany()
            .HasForeignKey(x => x.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ScenarioId);

        builder.HasIndex(x => new
        {
            x.ScenarioId,
            x.Order
        }).IsUnique();
    }
}
