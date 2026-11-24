using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class ForecastRunConfiguration : IEntityTypeConfiguration<ForecastRun>
{
    public void Configure(EntityTypeBuilder<ForecastRun> builder)
    {
        builder.ToTable("forecast_runs");

        builder.Property(run => run.Id).HasColumnName("id");
        builder.Property(run => run.StationId).HasColumnName("station_id");
        builder.Property(run => run.IssueTimeLocal)
            .HasColumnName("issue_time_local")
            .HasColumnType("timestamp without time zone");
        builder.Property(run => run.ModelBundleHash).HasColumnName("model_bundle_hash");
        builder.Property(run => run.InputHash).HasColumnName("input_hash");
        builder.Property(run => run.Status).HasColumnName("status");
        builder.Property(run => run.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(run => run.Station)
            .WithMany()
            .HasForeignKey(run => run.StationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
