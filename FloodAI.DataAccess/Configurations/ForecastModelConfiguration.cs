using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class ForecastModelConfiguration : IEntityTypeConfiguration<ForecastModel>
{
    public void Configure(EntityTypeBuilder<ForecastModel> builder)
    {
        builder.ToTable("forecast_models");

        builder.Property(model => model.Id).HasColumnName("id");
        builder.Property(model => model.StationId).HasColumnName("station_id");
        builder.Property(model => model.Version).HasColumnName("version");
        builder.Property(model => model.HorizonHours).HasColumnName("horizon_hours");
        builder.Property(model => model.ArtifactPath).HasColumnName("artifact_path");
        builder.Property(model => model.ManifestHash).HasColumnName("manifest_hash");
        builder.Property(model => model.IsActive).HasColumnName("is_active");

        builder.HasIndex(model => new { model.StationId, model.Version, model.HorizonHours }).IsUnique();

        builder.HasOne(model => model.Station)
            .WithMany()
            .HasForeignKey(model => model.StationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
