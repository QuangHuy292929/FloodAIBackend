using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("areas");

        builder.Property(area => area.Id).HasColumnName("id");
        builder.Property(area => area.ParentId).HasColumnName("parent_id");
        builder.Property(area => area.Code).HasColumnName("code");
        builder.Property(area => area.Name).HasColumnName("name");
        builder.Property(area => area.Level).HasColumnName("level");
        builder.Property(area => area.IsActive).HasColumnName("is_active");

        builder.HasIndex(area => area.Code).IsUnique();

        builder.HasOne(area => area.Parent)
            .WithMany(area => area.Children)
            .HasForeignKey(area => area.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
