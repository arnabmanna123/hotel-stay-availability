using HotelStay.Api.Domain;

namespace HotelStay.Api.Providers;

public interface IHotelProvider
{
    ProviderId Id { get; }

    Task<IReadOnlyList<Room>> SearchAsync(SearchQuery query, CancellationToken ct);
}
