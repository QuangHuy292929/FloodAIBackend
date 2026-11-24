using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class WarningEvaluationConfiguration : IEntityTypeConfiguration<WarningEvaluation>
{
    public void Configure(EntityTypeBuilder<WarningEvaluation> builder)
    {
        builder.ToTable("warning_evaluations");

        builder.Property(evaluation => evaluation.Id).HasColumnName("id");
        builder.Property(evaluation => evaluation.ForecastValueId).HasColumnName("forecast_value_id");
        builder.Property(evaluation => evaluation.ThresholdId).HasColumnName("threshold_id");
        builder.Property(evaluation => evaluation.Status).HasColumnName("status");
        builder.Property(evaluation => evaluation.EvaluatedAt)
            .HasColumnName("evaluated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(evaluation => evaluation.ForecastValue)
            .WithMany()
            .HasForeignKey(evaluation => evaluation.ForecastValueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(evaluation => evaluation.Threshold)
            .WithMany()
            .HasForeignKey(evaluation => evaluation.ThresholdId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
