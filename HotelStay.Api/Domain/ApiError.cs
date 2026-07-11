namespace HotelStay.Api.Domain;

public record ApiError(string Error, string Code);

public static class ErrorCodes
{
    public const string MissingDestination = "missing_destination";
    public const string MissingCheckIn = "missing_check_in";
    public const string MissingCheckOut = "missing_check_out";
    public const string InvalidDates = "invalid_dates";
    public const string UnknownDestination = "unknown_destination";
    public const string InvalidRoomType = "invalid_room_type";
    public const string InvalidBody = "invalid_body";
    public const string ValidationFailure = "validation_failure";
    public const string NotFound = "not_found";
    public const string RoomNotFound = "room_not_found";
    public const string UnknownProvider = "unknown_provider";
    public const string DocumentRequiredPassport = "document_required_passport";
    public const string DocumentNumberRequired = "document_number_required";
    public const string DocumentTypeRequired = "document_type_required";
    public const string GuestNameRequired = "guest_name_required";
    public const string ReservationNotFound = "reservation_not_found";
}
