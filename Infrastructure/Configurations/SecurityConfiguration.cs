using InvestManagement.Domain.Model;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace InvestManagement.Infrastructure.Configurations;

public class SecurityConfiguration
{
    public void Configure(EntityTypeBuilder<Security> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Ticker)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(x => x.Ticker)
            .IsUnique();
    }
}
