namespace PharmacyAvailability.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PharmacyAvailabilityPlatform";
    public string Audience { get; set; } = "PharmacyAvailabilityClients";
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 7;
}
