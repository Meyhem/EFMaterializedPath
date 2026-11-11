namespace EFMaterializedPath;

/// <summary>
/// Interface to provide serialization/deserialization logic for custom identifier types 
/// </summary>
/// <remarks>
/// The serialized text is stored in the entity's path between <c>|</c> separators, so it must not contain
/// <c>|</c>, must be the same on every machine (culture-independent), and two different ids must not serialize to
/// texts that differ only by letter case, because some databases compare paths ignoring case.
/// </remarks>
/// <typeparam name="TId"></typeparam>
public interface IIdentifierSerializer<TId>
{
    string SerializeIdentifier(TId id);
    TId DeserializeIdentifier(string id);
}
