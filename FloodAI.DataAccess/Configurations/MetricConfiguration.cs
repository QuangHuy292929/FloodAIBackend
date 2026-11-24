using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class MetricConfiguration : IEntityTypeConfiguration<Metric>
{
    public void Configure(EntityTypeBuilder<Metric> builder)
    {
        builder.ToTable("metrics");

        builder.Property(metric => metric.Id).HasColumnName("id");
        builder.Property(metric => metric.Code).HasColumnName("code");
        builder.Property(metric => metric.Name).HasColumnName("name");
        builder.Property(metric => metric.QuantityType).HasColumnName("quantity_type");

        builder.HasIndex(metric => metric.Code).IsUnique();
    }
}
