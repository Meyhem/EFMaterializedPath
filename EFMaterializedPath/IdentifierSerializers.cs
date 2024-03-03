using System;

namespace EFMaterializedPath;

public class IntIdentifierSerializer : IIdentifierSerializer<int>
{
    public string SerializeIdentifier(int id) => id.ToString();
    public int DeserializeIdentifier(string id) => int.Parse(id);
}

public class StringIdentifierSerializer : IIdentifierSerializer<string>
{
    public string SerializeIdentifier(string id) => id;
    public string DeserializeIdentifier(string id) => id;
}

public class GuidIdentifierSerializer : IIdentifierSerializer<Guid>
{
    public string SerializeIdentifier(Guid id) => id.ToString();
    public Guid DeserializeIdentifier(string id) => Guid.Parse(id);
}
