using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FloodAI.Business.Catalog.Admin;

internal static class CatalogAudit
{
    internal static void Record(FloodAiDbContext dbContext, long adminId, string action,
        string resourceType, string resourceKey) =>
        dbContext.CatalogActions.Add(new CatalogAction
        {
            AdminId = adminId, Action = action, ResourceType = resourceType, ResourceKey = resourceKey
        });

    internal static async Task SaveAsync(FloodAiDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new CatalogException(CatalogError.Conflict,
                "The catalog key is already in use or its referenced record changed.", error);
        }
    }
}
