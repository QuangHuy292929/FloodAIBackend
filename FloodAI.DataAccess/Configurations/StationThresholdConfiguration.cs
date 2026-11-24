using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class StationThresholdConfiguration : IEntityTypeConfiguration<StationThreshold>
{
    public void Configure(EntityTypeBuilder<StationThreshold> builder)
    {
        builder.ToTable("station_thresholds");

        builder.Property(threshold => threshold.Id).HasColumnName("id");
        builder.Property(threshold => threshold.StationId).HasColumnName("station_id");
        builder.Property(threshold => threshold.ThresholdType).HasColumnName("threshold_type");
        builder.Property(threshold => threshold.ValueRaw).HasColumnName("value_raw");
        builder.Property(threshold => threshold.Source).HasColumnName("source");
        builder.Property(threshold => threshold.UnitStatus).HasColumnName("unit_status");
        builder.Property(threshold => threshold.IsActive).HasColumnName("is_active");

        builder.HasOne(threshold => threshold.Station)
            .WithMany()
            .HasForeignKey(threshold => threshold.StationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
