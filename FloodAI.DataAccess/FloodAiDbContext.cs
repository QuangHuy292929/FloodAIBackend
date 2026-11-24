using Microsoft.EntityFrameworkCore;

namespace FloodAI.DataAccess;

public sealed class FloodAiDbContext(DbContextOptions<FloodAiDbContext> options)
    : DbContext(options)
{
}
