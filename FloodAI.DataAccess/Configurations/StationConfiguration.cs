using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class StationConfiguration : IEntityTypeConfiguration<Station>
{
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.ToTable("stations");

        builder.Property(station => station.Id).HasColumnName("id");
        builder.Property(station => station.BasinId).HasColumnName("basin_id");
        builder.Property(station => station.AreaId).HasColumnName("area_id");
        builder.Property(station => station.Code).HasColumnName("code");
        builder.Property(station => station.Name).HasColumnName("name");
        builder.Property(station => station.Type).HasColumnName("type");
        builder.Property(station => station.Latitude).HasColumnName("lat");
        builder.Property(station => station.Longitude).HasColumnName("lon");

        builder.HasIndex(station => new { station.BasinId, station.Code }).IsUnique();

        builder.HasOne(station => station.Basin)
            .WithMany(basin => basin.Stations)
            .HasForeignKey(station => station.BasinId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(station => station.Area)
            .WithMany(area => area.Stations)
            .HasForeignKey(station => station.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
