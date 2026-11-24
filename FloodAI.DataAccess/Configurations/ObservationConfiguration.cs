using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.ToTable("observations");

        builder.Property(observation => observation.Id).HasColumnName("id");
        builder.Property(observation => observation.StationId).HasColumnName("station_id");
        builder.Property(observation => observation.MetricId).HasColumnName("metric_id");
        builder.Property(observation => observation.SourceId).HasColumnName("source_id");
        builder.Property(observation => observation.TimestampLocal)
            .HasColumnName("timestamp_local")
            .HasColumnType("timestamp without time zone");
        builder.Property(observation => observation.ValueRaw).HasColumnName("value_raw");
        builder.Property(observation => observation.QualityStatus).HasColumnName("quality_status");
        builder.Property(observation => observation.AvailableAt).HasColumnName("available_at");

        builder.HasIndex(observation => new
        {
            observation.StationId,
            observation.MetricId,
            observation.SourceId,
            observation.TimestampLocal
        }).IsUnique();

        builder.HasOne(observation => observation.Station)
            .WithMany()
            .HasForeignKey(observation => observation.StationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(observation => observation.Metric)
            .WithMany()
            .HasForeignKey(observation => observation.MetricId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(observation => observation.Source)
            .WithMany()
            .HasForeignKey(observation => observation.SourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
