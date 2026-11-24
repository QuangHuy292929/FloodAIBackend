using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class ForecastValueConfiguration : IEntityTypeConfiguration<ForecastValue>
{
    public void Configure(EntityTypeBuilder<ForecastValue> builder)
    {
        builder.ToTable("forecast_values");

        builder.Property(value => value.Id).HasColumnName("id");
        builder.Property(value => value.RunId).HasColumnName("run_id");
        builder.Property(value => value.ModelId).HasColumnName("model_id");
        builder.Property(value => value.TargetTimeLocal)
            .HasColumnName("target_time_local")
            .HasColumnType("timestamp without time zone");
        builder.Property(value => value.HorizonHours).HasColumnName("horizon_hours");
        builder.Property(value => value.PredictedValueRaw).HasColumnName("predicted_value_raw");

        builder.HasIndex(value => new { value.RunId, value.HorizonHours }).IsUnique();

        builder.HasOne(value => value.Run)
            .WithMany(related => related.Values)
            .HasForeignKey(value => value.RunId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(value => value.Model)
            .WithMany()
            .HasForeignKey(value => value.ModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
