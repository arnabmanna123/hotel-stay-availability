using HotelStay.Api.Domain;

namespace HotelStay.Api.Services;

public interface IDocumentValidator
{
    /// <returns>Null when valid; otherwise the ApiError to return with a 422 response.</returns>
    ApiError? Validate(CityClass cityClass, DocumentType documentType, string? documentNumber);
}

public sealed class DocumentValidator : IDocumentValidator
{
    public ApiError? Validate(CityClass cityClass, DocumentType documentType, string? documentNumber)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            return new ApiError("Document number is required", ErrorCodes.DocumentNumberRequired);
        }

        if (cityClass == CityClass.International && documentType != DocumentType.Passport)
        {
            return new ApiError(
                "Passport required for international destinations",
                ErrorCodes.DocumentRequiredPassport);
        }

        // Domestic accepts either Passport or NationalId (spec §5.1).
        return null;
    }
}
