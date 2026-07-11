namespace HotelStay.Api.Domain;

public record CancellationPolicy(CancellationPolicyType Type, int? HoursBeforeCheckIn);
