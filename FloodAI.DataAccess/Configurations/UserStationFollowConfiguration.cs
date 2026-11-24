using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class UserStationFollowConfiguration : IEntityTypeConfiguration<UserStationFollow>
{
    public void Configure(EntityTypeBuilder<UserStationFollow> builder)
    {
        builder.ToTable("user_station_follows");

        builder.Property(follow => follow.UserId).HasColumnName("user_id");
        builder.Property(follow => follow.StationId).HasColumnName("station_id");
        builder.Property(follow => follow.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasKey(follow => new { follow.UserId, follow.StationId });

        builder.HasOne(follow => follow.User)
            .WithMany()
            .HasForeignKey(follow => follow.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(follow => follow.Station)
            .WithMany()
            .HasForeignKey(follow => follow.StationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
