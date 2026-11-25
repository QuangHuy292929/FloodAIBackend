using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class CatalogActionConfiguration : IEntityTypeConfiguration<CatalogAction>
{
    public void Configure(EntityTypeBuilder<CatalogAction> builder)
    {
        builder.ToTable("catalog_actions");

        builder.Property(action => action.Id).HasColumnName("id");
        builder.Property(action => action.AdminId).HasColumnName("admin_id");
        builder.Property(action => action.Action).HasColumnName("action");
        builder.Property(action => action.ResourceType).HasColumnName("resource_type");
        builder.Property(action => action.ResourceKey).HasColumnName("resource_key");
        builder.Property(action => action.CreatedAt).HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(action => new { action.CreatedAt, action.Id });
        builder.HasOne(action => action.Admin).WithMany()
            .HasForeignKey(action => action.AdminId).OnDelete(DeleteBehavior.Restrict);
    }
}
