using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.DataAccess;

public sealed class FloodAiDbContext(DbContextOptions<FloodAiDbContext> options)
    : DbContext(options)
{
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Basin> Basins => Set<Basin>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<Observation> Observations => Set<Observation>();
    public DbSet<ForecastModel> ForecastModels => Set<ForecastModel>();
    public DbSet<ForecastRun> ForecastRuns => Set<ForecastRun>();
    public DbSet<ForecastValue> ForecastValues => Set<ForecastValue>();
    public DbSet<StationThreshold> StationThresholds => Set<StationThreshold>();
    public DbSet<WarningEvaluation> WarningEvaluations => Set<WarningEvaluation>();
    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();
    public DbSet<PostMedia> PostMedia => Set<PostMedia>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<ModerationAction> ModerationActions => Set<ModerationAction>();
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();
    public DbSet<UserAreaFollow> UserAreaFollows => Set<UserAreaFollow>();
    public DbSet<UserStationFollow> UserStationFollows => Set<UserStationFollow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FloodAiDbContext).Assembly);
    }
}
