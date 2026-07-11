using HotelStay.Api.Domain;
using HotelStay.Api.Services;

namespace HotelStay.Tests.Services;

public class DocumentValidatorTests
{
    private readonly DocumentValidator _validator = new();

    // The full domestic × international × Passport × NationalId × present × empty truth table.
    // Spec §5.1: international requires Passport; domestic accepts either.

    [Theory]
    [InlineData(CityClass.Domestic, DocumentType.NationalId, "ID-123", null)]
    [InlineData(CityClass.Domestic, DocumentType.Passport, "PP-123", null)]
    [InlineData(CityClass.International, DocumentType.Passport, "PP-123", null)]
    public void Accepts_valid_combinations(CityClass city, DocumentType doc, string number, string? expectedCode)
    {
        var result = _validator.Validate(city, doc, number);
        Assert.Null(result);
        Assert.Null(expectedCode);
    }

    [Theory]
    [InlineData(CityClass.International, DocumentType.NationalId, "ID-123", ErrorCodes.DocumentRequiredPassport)]
    public void Rejects_national_id_for_international(CityClass city, DocumentType doc, string number, string expectedCode)
    {
        var result = _validator.Validate(city, doc, number);
        Assert.NotNull(result);
        Assert.Equal(expectedCode, result!.Code);
    }

    [Theory]
    [InlineData(CityClass.Domestic, DocumentType.NationalId, "", ErrorCodes.DocumentNumberRequired)]
    [InlineData(CityClass.Domestic, DocumentType.NationalId, "   ", ErrorCodes.DocumentNumberRequired)]
    [InlineData(CityClass.Domestic, DocumentType.NationalId, null, ErrorCodes.DocumentNumberRequired)]
    [InlineData(CityClass.International, DocumentType.Passport, "", ErrorCodes.DocumentNumberRequired)]
    public void Rejects_missing_or_blank_document_number(
        CityClass city,
        DocumentType doc,
        string? number,
        string expectedCode)
    {
        var result = _validator.Validate(city, doc, number);
        Assert.NotNull(result);
        Assert.Equal(expectedCode, result!.Code);
    }
}
