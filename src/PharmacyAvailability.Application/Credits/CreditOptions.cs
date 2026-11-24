namespace PharmacyAvailability.Application.Credits;

public class CreditOptions
{
    public const string SectionName = "Credits";

    /// <summary>Master switch. While false nothing is ever charged and queries stay open (free launch period). Usage is still logged.</summary>
    public bool ChargingEnabled { get; set; }

    /// <summary>Queries that cost nothing per new account once charging is on.</summary>
    public int FreeTrialQueries { get; set; } = 10;

    /// <summary>Points charged per successful query.</summary>
    public int PointsPerQuery { get; set; } = 1;

    /// <summary>Max queries per signed-in user per UTC day, in every mode. 0 turns the cap off.</summary>
    public int DailyQueryCapPerUser { get; set; }

    public long MaxTopUpPoints { get; set; } = 100_000;
}
