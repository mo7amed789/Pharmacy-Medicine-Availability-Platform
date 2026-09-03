using PharmacyAvailability.Domain.Common;
using Xunit;

namespace PharmacyAvailability.UnitTests.Availability;

public class AvailabilityHaversineTests
{
    [Fact]
    public void CalculateDistance_IdenticalCoordinates_ReturnsZero()
    {
        double lat = 30.0444;
        double lon = 31.2357;

        var distance = GeospatialUtils.CalculateDistanceKm(lat, lon, lat, lon);

        Assert.Equal(0.0, distance);
    }

    [Fact]
    public void CalculateDistance_KnownCoordinates_CairoTahrirToMaadi_MatchesGeodesicExpectation()
    {
        // Tahrir Square: 30.0444, 31.2357
        // Maadi: 29.9602, 31.2569
        // Approximate distance: ~9.6 km
        double tahrirLat = 30.0444;
        double tahrirLon = 31.2357;
        double maadiLat = 29.9602;
        double maadiLon = 31.2569;

        var distance = GeospatialUtils.CalculateDistanceKm(tahrirLat, tahrirLon, maadiLat, maadiLon);

        // Distance should be approximately 9.6 km (within 0.5 km tolerance)
        Assert.InRange(distance, 9.2, 10.0);
    }

    [Fact]
    public void CalculateDistance_KnownCoordinates_CairoToAlexandria_MatchesGeodesicExpectation()
    {
        // Cairo: 30.0444, 31.2357
        // Alexandria: 31.2001, 29.9187
        // Approximate straight-line distance: ~180 km
        double cairoLat = 30.0444;
        double cairoLon = 31.2357;
        double alexLat = 31.2001;
        double alexLon = 29.9187;

        var distance = GeospatialUtils.CalculateDistanceKm(cairoLat, cairoLon, alexLat, alexLon);

        Assert.InRange(distance, 175.0, 190.0);
    }

    [Fact]
    public void CalculateBoundingBox_ReturnsValidMinMaxBounds()
    {
        double centerLat = 30.0444;
        double centerLon = 31.2357;
        double radiusKm = 10.0;

        var (minLat, maxLat, minLon, maxLon) = GeospatialUtils.CalculateBoundingBox(centerLat, centerLon, radiusKm);

        Assert.True(minLat < centerLat);
        Assert.True(maxLat > centerLat);
        Assert.True(minLon < centerLon);
        Assert.True(maxLon > centerLon);

        // Within 10km, deltaLat is approximately 10/111 ~ 0.09 degrees
        Assert.InRange(centerLat - minLat, 0.08, 0.10);
        Assert.InRange(maxLat - centerLat, 0.08, 0.10);
    }

    [Fact]
    public void CalculateBoundingBox_NearPoles_ClampsCorrectly()
    {
        double centerLat = 89.9;
        double centerLon = 0.0;
        double radiusKm = 50.0;

        var (minLat, maxLat, minLon, maxLon) = GeospatialUtils.CalculateBoundingBox(centerLat, centerLon, radiusKm);

        Assert.True(maxLat <= 90.0);
        Assert.True(minLat >= -90.0);
        Assert.True(minLon >= -180.0);
        Assert.True(maxLon <= 180.0);
    }
}
