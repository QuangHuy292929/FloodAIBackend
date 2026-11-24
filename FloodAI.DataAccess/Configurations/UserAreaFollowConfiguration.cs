using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class UserAreaFollowConfiguration : IEntityTypeConfiguration<UserAreaFollow>
{
    public void Configure(EntityTypeBuilder<UserAreaFollow> builder)
    {
        builder.ToTable("user_area_follows");

        builder.Property(follow => follow.UserId).HasColumnName("user_id");
        builder.Property(follow => follow.AreaId).HasColumnName("area_id");
        builder.Property(follow => follow.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasKey(follow => new { follow.UserId, follow.AreaId });

        builder.HasOne(follow => follow.User)
            .WithMany()
            .HasForeignKey(follow => follow.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(follow => follow.Area)
            .WithMany()
            .HasForeignKey(follow => follow.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
