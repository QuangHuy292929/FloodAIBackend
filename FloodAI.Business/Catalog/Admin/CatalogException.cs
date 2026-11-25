namespace FloodAI.Business.Catalog.Admin;

public enum CatalogError
{
    Invalid,
    NotFound,
    Conflict
}

public sealed class CatalogException(CatalogError error, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public CatalogError Error { get; } = error;
}
