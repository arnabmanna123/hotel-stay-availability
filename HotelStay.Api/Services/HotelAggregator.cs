using HotelStay.Api.Domain;
using HotelStay.Api.Providers;

namespace HotelStay.Api.Services;

public sealed class HotelAggregator
{
    private readonly IEnumerable<IHotelProvider> _providers;
    private readonly ILogger<HotelAggregator> _logger;

    public HotelAggregator(IEnumerable<IHotelProvider> providers, ILogger<HotelAggregator> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Room>> SearchAsync(SearchQuery query, CancellationToken ct)
    {
        var tasks = _providers.Select(p => SafeSearchAsync(p, query, ct)).ToArray();
        var perProvider = await Task.WhenAll(tasks);

        return perProvider
            .SelectMany(rooms => rooms)
            .OrderBy(r => r.TotalPrice)
            .ThenBy(r => r.ProviderId)
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<Room?> FindRoomAsync(
        ProviderId providerId,
        string roomId,
        SearchQuery query,
        CancellationToken ct)
    {
        var provider = _providers.FirstOrDefault(p => p.Id == providerId);
        if (provider is null) return null;

        var rooms = await SafeSearchAsync(provider, query, ct);
        return rooms.FirstOrDefault(r => string.Equals(r.Id, roomId, StringComparison.Ordinal));
    }

    private async Task<IReadOnlyList<Room>> SafeSearchAsync(
        IHotelProvider provider,
        SearchQuery query,
        CancellationToken ct)
    {
        try
        {
            return await provider.SearchAsync(query, ct);
        }
        catch (Exception ex)
        {
            // A single provider failing must not fail the whole search — spec §3.
            _logger.LogError(ex, "Provider {Provider} failed for destination={Destination}",
                provider.Id, query.Destination);
            return Array.Empty<Room>();
        }
    }
}
