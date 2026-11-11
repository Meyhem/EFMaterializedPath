using System.Globalization;

namespace EFMaterializedPath.Test;

public class TestIdentifierSerializers
{
    [Fact]
    public void IntSerializerIgnoresCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "~";
        try
        {
            CultureInfo.CurrentCulture = culture;
            var serializer = new IntIdentifierSerializer();

            serializer.SerializeIdentifier(-5).Should().Be("-5");
            serializer.DeserializeIdentifier("-5").Should().Be(-5);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void IntSerializerReadsMinusSignWrittenByOlderVersions()
    {
        // Before the invariant-culture fix, cultures such as sv-SE stored negative ids with U+2212 MINUS SIGN.
        new IntIdentifierSerializer().DeserializeIdentifier("−5").Should().Be(-5);
    }

    [Fact]
    public void GuidSerializerRoundTripsInDFormat()
    {
        var serializer = new GuidIdentifierSerializer();
        var id = Guid.NewGuid();

        serializer.SerializeIdentifier(id).Should().Be(id.ToString("D"));
        serializer.DeserializeIdentifier(serializer.SerializeIdentifier(id)).Should().Be(id);
    }
}
