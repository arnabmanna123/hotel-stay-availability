using System.Text.RegularExpressions;
using HotelStay.Api.Services;

namespace HotelStay.Tests.Services;

public class ReferenceNumberFactoryTests
{
    private static readonly Regex Format = new(@"^HS-[A-Z0-9]{8}$", RegexOptions.Compiled);

    [Fact]
    public void Generates_reference_matching_documented_format()
    {
        var factory = new ReferenceNumberFactory();
        var reference = factory.Generate();
        Assert.Matches(Format, reference);
    }

    [Fact]
    public void Ten_thousand_generations_produce_no_duplicates()
    {
        var factory = new ReferenceNumberFactory();
        var refs = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < 10_000; i++)
        {
            Assert.True(refs.Add(factory.Generate()), "Duplicate reference generated");
        }
    }
}
