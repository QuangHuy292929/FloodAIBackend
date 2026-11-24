using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class BasinConfiguration : IEntityTypeConfiguration<Basin>
{
    public void Configure(EntityTypeBuilder<Basin> builder)
    {
        builder.ToTable("basins");

        builder.Property(basin => basin.Id).HasColumnName("id");
        builder.Property(basin => basin.Code).HasColumnName("code");
        builder.Property(basin => basin.Name).HasColumnName("name");
        builder.Property(basin => basin.Description).HasColumnName("description");
        builder.Property(basin => basin.IsActive).HasColumnName("is_active");

        builder.HasIndex(basin => basin.Code).IsUnique();
    }
}
