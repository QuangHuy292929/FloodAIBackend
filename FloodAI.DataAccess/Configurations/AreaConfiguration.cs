using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("areas", table => table.HasCheckConstraint("CK_areas_weather_coordinates",
            "(weather_latitude IS NULL AND weather_longitude IS NULL) " +
            "OR (weather_latitude IS NOT NULL AND weather_longitude IS NOT NULL " +
            "AND weather_latitude BETWEEN -90 AND 90 AND weather_longitude BETWEEN -180 AND 180)"));

        builder.Property(area => area.Id).HasColumnName("id");
        builder.Property(area => area.ParentId).HasColumnName("parent_id");
        builder.Property(area => area.Code).HasColumnName("code");
        builder.Property(area => area.Name).HasColumnName("name");
        builder.Property(area => area.Level).HasColumnName("level");
        builder.Property(area => area.IsActive).HasColumnName("is_active");
        builder.Property(area => area.WeatherLatitude).HasColumnName("weather_latitude");
        builder.Property(area => area.WeatherLongitude).HasColumnName("weather_longitude");

        builder.HasIndex(area => area.Code).IsUnique();

        builder.HasOne(area => area.Parent)
            .WithMany(area => area.Children)
            .HasForeignKey(area => area.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
