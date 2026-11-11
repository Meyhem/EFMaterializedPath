using System;
using System.Globalization;

namespace EFMaterializedPath;

public class IntIdentifierSerializer : IIdentifierSerializer<int>
{
    public string SerializeIdentifier(int id) => id.ToString(CultureInfo.InvariantCulture);
    // Also accepts U+2212 MINUS SIGN, which versions before the invariant-culture fix wrote under cultures such as sv-SE.
    public int DeserializeIdentifier(string id) =>
        int.Parse(id.Replace('\u2212', '-'), CultureInfo.InvariantCulture);
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
