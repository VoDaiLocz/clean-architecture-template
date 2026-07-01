namespace Application.Common.Interfaces.Storage;

public interface IObjectStorage
{
    void Put(PutObjectRequest request);

    StoredObject? Get(ObjectKey key);

    IReadOnlyList<string> List(string prefix);

    void Delete(ObjectKey key);
}

public sealed record ObjectKey
{
    public ObjectKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Object key is required.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }
}

public sealed record PutObjectRequest
{
    public PutObjectRequest(ObjectKey key, string contentType, byte[] content)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        Key = key;
        ContentType = contentType;
        Content = content;
    }

    public ObjectKey Key { get; }

    public string ContentType { get; }

    public byte[] Content { get; }
}

public sealed record StoredObject(ObjectKey Key, string ContentType, byte[] Content);
