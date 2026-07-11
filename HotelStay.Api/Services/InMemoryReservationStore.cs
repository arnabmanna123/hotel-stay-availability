using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelStay.Api.Domain;
using Microsoft.Extensions.Configuration;

namespace HotelStay.Api.Services;

public interface IReservationStore
{
    bool TrySave(Reservation reservation);
    Reservation? Find(string reference);
}

public sealed class InMemoryReservationStore : IReservationStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
    };

    private readonly ConcurrentDictionary<string, Reservation> _byReference =
        new(StringComparer.Ordinal);

    private readonly string _path;
    private readonly object _fileLock = new();

    public InMemoryReservationStore(IConfiguration configuration)
    {
        var configuredPath = configuration["ReservationsFilePath"];
        _path = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "reservations.json")
            : Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(configuredPath, AppContext.BaseDirectory);

        if (!File.Exists(_path))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_path);
            var reservations = JsonSerializer.Deserialize<IEnumerable<Reservation>>(json, SerializerOptions);
            if (reservations is null)
            {
                return;
            }

            foreach (var reservation in reservations)
            {
                _byReference[reservation.Reference] = reservation;
            }
        }
        catch
        {
            // If persistence fails on startup, continue with an empty in-memory store.
        }
    }

    public bool TrySave(Reservation reservation)
    {
        if (!_byReference.TryAdd(reservation.Reference, reservation))
        {
            return false;
        }

        Persist();
        return true;
    }

    public Reservation? Find(string reference) =>
        _byReference.TryGetValue(reference, out var reservation) ? reservation : null;

    private void Persist()
    {
        lock (_fileLock)
        {
            var json = JsonSerializer.Serialize(_byReference.Values, SerializerOptions);
            File.WriteAllText(_path, json);
        }
    }
}
