using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class RiverBranchConfiguration : IEntityTypeConfiguration<RiverBranch>
{
    public void Configure(EntityTypeBuilder<RiverBranch> builder)
    {
        builder.ToTable("river_branches");
        builder.Property(branch => branch.Id).HasColumnName("id");
        builder.Property(branch => branch.BasinId).HasColumnName("basin_id");
        builder.Property(branch => branch.Code).HasColumnName("code");
        builder.Property(branch => branch.Name).HasColumnName("name");
        builder.HasIndex(branch => new { branch.BasinId, branch.Code }).IsUnique();
        builder.HasOne(branch => branch.Basin).WithMany(basin => basin.RiverBranches)
            .HasForeignKey(branch => branch.BasinId).OnDelete(DeleteBehavior.Restrict);
    }
}
