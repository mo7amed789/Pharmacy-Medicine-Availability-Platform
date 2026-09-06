namespace PharmacyAvailability.Application.Reservations;

public class ReservationOptions
{
    public const string SectionName = "Reservations";

    public int HoldDurationMinutes { get; set; } = 60;
    public int ExpirationWorkerIntervalSeconds { get; set; } = 60;
}
