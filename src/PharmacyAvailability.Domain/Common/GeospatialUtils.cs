namespace PharmacyAvailability.Domain.Common;

public static class GeospatialUtils
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Computes the great-circle distance between two GPS coordinates using the Haversine formula.
    /// </summary>
    public static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double rLat1 = ToRadians(lat1);
        double rLat2 = ToRadians(lat2);

        double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                   Math.Cos(rLat1) * Math.Cos(rLat2) *
                   Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        return Math.Round(EarthRadiusKm * c, 2);
    }

    /// <summary>
    /// Computes an approximate rectangular bounding box for candidate set reduction in SQL queries.
    /// </summary>
    public static (double MinLat, double MaxLat, double MinLon, double MaxLon) CalculateBoundingBox(
        double centerLat,
        double centerLon,
        double radiusKm)
    {
        // 1 degree of latitude ~= 111.0 km
        double deltaLat = radiusKm / 111.0;

        // 1 degree of longitude ~= 111.0 * cos(lat) km
        double cosLat = Math.Cos(ToRadians(centerLat));
        double deltaLon = cosLat > 0.0001 ? radiusKm / (111.0 * cosLat) : radiusKm / 111.0;

        double minLat = Math.Max(-90.0, centerLat - deltaLat);
        double maxLat = Math.Min(90.0, centerLat + deltaLat);

        double minLon = Math.Max(-180.0, centerLon - deltaLon);
        double maxLon = Math.Min(180.0, centerLon + deltaLon);

        return (minLat, maxLat, minLon, maxLon);
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
