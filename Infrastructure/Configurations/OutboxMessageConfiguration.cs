using InvestManagement.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestManagement.Infrastructure.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Type)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(message => new
        {
            message.NextAttemptAtUtc,
            message.CreatedAtUtc
        })
            .HasFilter("\"ProcessedAtUtc\" IS NULL");
    }
}
