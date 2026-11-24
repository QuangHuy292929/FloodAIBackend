using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.DataAccess;

public sealed class FloodAiDbContext(DbContextOptions<FloodAiDbContext> options)
    : DbContext(options)
{
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Basin> Basins => Set<Basin>();
    public DbSet<Station> Stations => Set<Station>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FloodAiDbContext).Assembly);
    }
}
