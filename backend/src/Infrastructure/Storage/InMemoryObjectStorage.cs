using Application.Common.Interfaces.Storage;

namespace Infrastructure.Storage;

public sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly Dictionary<string, StoredObject> objects = new(StringComparer.Ordinal);

    public void Put(PutObjectRequest request)
    {
        objects[request.Key.Value] = new StoredObject(
            request.Key,
            request.ContentType,
            request.Content.ToArray()
        );
    }

    public StoredObject? Get(ObjectKey key)
    {
        return objects.TryGetValue(key.Value, out var stored)
            ? new StoredObject(stored.Key, stored.ContentType, stored.Content.ToArray())
            : null;
    }

    public IReadOnlyList<string> List(string prefix)
    {
        return objects.Keys
            .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    public void Delete(ObjectKey key)
    {
        objects.Remove(key.Value);
    }
}
