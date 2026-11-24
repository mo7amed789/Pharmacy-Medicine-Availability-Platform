using PharmacyAvailability.Domain.Common;

namespace PharmacyAvailability.Domain.Credits;

public enum QueryOutcome
{
    Success = 1,
    /// <summary>Every returned row had stale or unknown stock. Never charged.</summary>
    Unknown = 2,
    NoResults = 3
}

/// <summary>One row per availability query. Deliberately stores no search text, location or IP.</summary>
public class QueryUsageLog : BaseEntity
{
    public Guid? UserId { get; private set; }
    public QueryOutcome Outcome { get; private set; }
    public int ResultCount { get; private set; }
    public int PointsCharged { get; private set; }
    public bool UsedFreeQuery { get; private set; }

    protected QueryUsageLog() { }

    public QueryUsageLog(Guid? userId, QueryOutcome outcome, int resultCount, int pointsCharged, bool usedFreeQuery)
    {
        UserId = userId;
        Outcome = outcome;
        ResultCount = resultCount;
        PointsCharged = pointsCharged;
        UsedFreeQuery = usedFreeQuery;
    }
}
