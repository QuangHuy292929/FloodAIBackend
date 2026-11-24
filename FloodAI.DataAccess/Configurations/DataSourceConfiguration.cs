using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class DataSourceConfiguration : IEntityTypeConfiguration<DataSource>
{
    public void Configure(EntityTypeBuilder<DataSource> builder)
    {
        builder.ToTable("data_sources");

        builder.Property(source => source.Id).HasColumnName("id");
        builder.Property(source => source.Name).HasColumnName("name");
        builder.Property(source => source.Url).HasColumnName("url");
        builder.Property(source => source.SourceType).HasColumnName("source_type");
        builder.Property(source => source.Status).HasColumnName("status");
    }
}
