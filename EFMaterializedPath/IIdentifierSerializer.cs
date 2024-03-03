namespace EFMaterializedPath;

/// <summary>
/// Interface to provide serialization/deserialization logic for custom identifier types 
/// </summary>
/// <typeparam name="TId"></typeparam>
public interface IIdentifierSerializer<TId>
{
    string SerializeIdentifier(TId id);
    TId DeserializeIdentifier(string id);
}
