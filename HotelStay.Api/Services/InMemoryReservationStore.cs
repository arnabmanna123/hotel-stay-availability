using System.Collections.Concurrent;
using HotelStay.Api.Domain;

namespace HotelStay.Api.Services;

public interface IReservationStore
{
    void Save(Reservation reservation);
    Reservation? Find(string reference);
}

public sealed class InMemoryReservationStore : IReservationStore
{
    private readonly ConcurrentDictionary<string, Reservation> _byReference = new(StringComparer.Ordinal);

    public void Save(Reservation reservation) => _byReference[reservation.Reference] = reservation;

    public Reservation? Find(string reference) =>
        _byReference.TryGetValue(reference, out var reservation) ? reservation : null;
}
