using FloodAI.DataAccess.Entities;
using FloodAI.DataAccess.Observations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class DataSourceConfiguration : IEntityTypeConfiguration<DataSource>
{
    public void Configure(EntityTypeBuilder<DataSource> builder)
    {
        builder.ToTable("data_sources", table => table.HasCheckConstraint("ck_data_sources_time_basis",
            "time_basis IN ('SOURCE_LOCAL_UNVERIFIED', 'VIETNAM_LOCAL', 'UTC')"));

        builder.Property(source => source.Id).HasColumnName("id");
        builder.Property(source => source.Name).HasColumnName("name");
        builder.Property(source => source.Url).HasColumnName("url");
        builder.Property(source => source.SourceType).HasColumnName("source_type");
        builder.Property(source => source.Status).HasColumnName("status");
        builder.Property(source => source.TimeBasis).HasColumnName("time_basis")
            .HasDefaultValue(ObservationMetadata.SourceLocalUnverified);
        builder.Property(source => source.SupportsUpdates).HasColumnName("supports_updates");
    }
}
