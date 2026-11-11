using System;
using System.Globalization;

namespace EFMaterializedPath;

public class IntIdentifierSerializer : IIdentifierSerializer<int>
{
    public string SerializeIdentifier(int id) => id.ToString(CultureInfo.InvariantCulture);
    public int DeserializeIdentifier(string id) => int.Parse(id, CultureInfo.InvariantCulture);
}

public class StringIdentifierSerializer : IIdentifierSerializer<string>
{
    public string SerializeIdentifier(string id) => id;
    public string DeserializeIdentifier(string id) => id;
}

public class GuidIdentifierSerializer : IIdentifierSerializer<Guid>
{
    public string SerializeIdentifier(Guid id) => id.ToString("D");
    public Guid DeserializeIdentifier(string id) => Guid.Parse(id);
}
