using HotelStay.Api.Domain;
using HotelStay.Api.Providers.PremierStays;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotelStay.Tests.Providers;

public class PremierStaysProviderTests
{
    private static PremierStaysProvider Create() =>
        new(NullLogger<PremierStaysProvider>.Instance);

    [Fact]
    public async Task Returns_rooms_for_known_destination()
    {
        var provider = Create();
        var query = new SearchQuery("London", new DateTime(2026, 8, 1), new DateTime(2026, 8, 4), RoomType: null);

        var rooms = await provider.SearchAsync(query, CancellationToken.None);

        Assert.NotEmpty(rooms);
        Assert.All(rooms, r => Assert.Equal(ProviderId.PremierStays, r.ProviderId));
    }

    [Fact]
    public async Task Returns_empty_for_unknown_destination()
    {
        var provider = Create();
        var query = new SearchQuery("Atlantis", new DateTime(2026, 8, 1), new DateTime(2026, 8, 4), null);

        var rooms = await provider.SearchAsync(query, CancellationToken.None);

        Assert.Empty(rooms);
    }

    [Fact]
    public async Task RoomType_filter_narrows_results()
    {
        var provider = Create();
        var query = new SearchQuery("London", new DateTime(2026, 8, 1), new DateTime(2026, 8, 4), RoomType.Suite);

        var rooms = await provider.SearchAsync(query, CancellationToken.None);

        Assert.All(rooms, r => Assert.Equal(RoomType.Suite, r.RoomType));
    }

    [Fact]
    public async Task TotalPrice_is_perNight_times_nights()
    {
        var provider = Create();
        var query = new SearchQuery("Manchester", new DateTime(2026, 8, 1), new DateTime(2026, 8, 5), RoomType.Standard);

        var rooms = await provider.SearchAsync(query, CancellationToken.None);

        var room = Assert.Single(rooms);
        Assert.Equal(95m, room.PricePerNight);
        Assert.Equal(380m, room.TotalPrice); // 4 nights
    }

    [Fact]
    public void Normalise_maps_FreeCancellation_to_48_hours()
    {
        var dto = new PremierStaysRoomDto
        {
            Id = "PS-X",
            Destination = "Anywhere",
            RoomType = "Standard",
            PricePerNight = 100m,
            Cancellation = "FreeCancellation",
        };

        var room = PremierStaysProvider.Normalise(dto, nights: 2);

        Assert.Equal(CancellationPolicyType.FreeCancellation, room.CancellationPolicy.Type);
        Assert.Equal(48, room.CancellationPolicy.HoursBeforeCheckIn);
    }

    [Fact]
    public void Normalise_maps_NonRefundable_to_null_hours()
    {
        var dto = new PremierStaysRoomDto
        {
            Id = "PS-X",
            Destination = "Anywhere",
            RoomType = "Suite",
            PricePerNight = 200m,
            Cancellation = "NonRefundable",
            StarRating = 5,
            Amenities = new List<string> { "WiFi" },
        };

        var room = PremierStaysProvider.Normalise(dto, nights: 3);

        Assert.Equal(CancellationPolicyType.NonRefundable, room.CancellationPolicy.Type);
        Assert.Null(room.CancellationPolicy.HoursBeforeCheckIn);
        Assert.Equal(5, room.StarRating);
        Assert.Contains("WiFi", room.Amenities);
    }

    [Fact]
    public void Normalise_throws_for_unknown_cancellation_value()
    {
        var dto = new PremierStaysRoomDto
        {
            Id = "PS-X",
            Destination = "Anywhere",
            RoomType = "Standard",
            PricePerNight = 100m,
            Cancellation = "SomethingElse",
        };

        Assert.Throws<InvalidOperationException>(() => PremierStaysProvider.Normalise(dto, nights: 1));
    }
}
