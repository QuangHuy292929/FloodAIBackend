using FloodAI.Business.Community;

namespace FloodAI.Api.Community;

public sealed class LocalCommunityMediaStore(IConfiguration configuration, ILogger<LocalCommunityMediaStore> logger)
    : ICommunityMediaStore
{
    private readonly string root = Path.GetFullPath(configuration["CommunityMedia:RootPath"] ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FloodAI", "community-media"));

    public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken)
    {
        var path = PathFor(storageKey);
        Directory.CreateDirectory(root);
        var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true);
        try
        {
            await using (file)
            {
                await content.CopyToAsync(file, cancellationToken);
            }
        }
        catch
        {
            Delete(storageKey);
            throw;
        }
    }

    public Stream? OpenRead(string storageKey)
    {
        try
        {
            return File.OpenRead(PathFor(storageKey));
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    public void Delete(string storageKey)
    {
        try
        {
            File.Delete(PathFor(storageKey));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not remove community media from local storage.");
        }
    }

    private string PathFor(string storageKey) => Guid.TryParseExact(storageKey, "N", out _)
        ? Path.Combine(root, storageKey)
        : throw new InvalidOperationException("Invalid community media storage key.");
}
