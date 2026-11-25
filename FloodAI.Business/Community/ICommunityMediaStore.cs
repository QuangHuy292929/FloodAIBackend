namespace FloodAI.Business.Community;

public interface ICommunityMediaStore
{
    Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken);
    Stream? OpenRead(string storageKey);
    void Delete(string storageKey);
}
