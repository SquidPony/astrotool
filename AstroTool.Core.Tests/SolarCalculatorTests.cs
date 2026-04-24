using AstroTool.Core.Astronomy;
using AstroTool.Core.Models;
using Xunit;

namespace AstroTool.Core.Tests;

public class SolarCalculatorTests
{
    // Greenwich, UK
    private const double Lat = 51.5;
    private const double Lon = 0.0;
    private const double J2000 = 2451545.0;

    [Fact]
    public void GetSunPosition_ReturnsNonNullResult()
    {
        SkyPosition pos = SolarCalculator.GetSunPosition(J2000, Lat, Lon);
        Assert.NotNull(pos);
    }

    [Fact]
    public void GetSunPosition_AltitudeInValidRange()
    {
        SkyPosition pos = SolarCalculator.GetSunPosition(J2000, Lat, Lon);
        Assert.InRange(pos.Altitude, -90.0, 90.0);
    }

    [Fact]
    public void GetSunPosition_AzimuthInValidRange()
    {
        SkyPosition pos = SolarCalculator.GetSunPosition(J2000, Lat, Lon);
        Assert.InRange(pos.Azimuth, 0.0, 360.0);
    }

    [Fact]
    public void GetSunPosition_RightAscensionInValidRange()
    {
        SkyPosition pos = SolarCalculator.GetSunPosition(J2000, Lat, Lon);
        Assert.InRange(pos.RightAscension, 0.0, 24.0);
    }

    [Fact]
    public void GetSunPosition_DeclinationInValidRange()
    {
        SkyPosition pos = SolarCalculator.GetSunPosition(J2000, Lat, Lon);
        // Sun's declination stays within ±23.44° (Earth's axial tilt)
        Assert.InRange(pos.Declination, -23.5, 23.5);
    }

    [Fact]
    public void GetSunPosition_AtSummerSolstice_HighDeclination()
    {
        // Summer solstice 2000: ~June 21
        var solstice = new DateTime(2000, 6, 21, 12, 0, 0, DateTimeKind.Utc);
        double jd = AstroTime.ToJulianDate(solstice);
        SkyPosition pos = SolarCalculator.GetSunPosition(jd, Lat, Lon);
        // Dec should be near +23.44° at summer solstice
        Assert.InRange(pos.Declination, 22.0, 24.0);
    }

    [Fact]
    public void GetSunPosition_AtWinterSolstice_LowDeclination()
    {
        // Winter solstice: ~Dec 21
        var solstice = new DateTime(2000, 12, 21, 12, 0, 0, DateTimeKind.Utc);
        double jd = AstroTime.ToJulianDate(solstice);
        SkyPosition pos = SolarCalculator.GetSunPosition(jd, Lat, Lon);
        // Dec should be near −23.44° at winter solstice
        Assert.InRange(pos.Declination, -24.0, -22.0);
    }

    [Theory]
    [InlineData(60.0, 0.0)]    // Oslo
    [InlineData(0.0, 0.0)]    // Equator
    [InlineData(-33.9, 151.2)] // Sydney
    [InlineData(35.7, 139.7)]  // Tokyo
    public void GetSunPosition_DifferentLocations_AltitudeInRange(double lat, double lon)
    {
        SkyPosition pos = SolarCalculator.GetSunPosition(J2000, lat, lon);
        Assert.InRange(pos.Altitude, -90.0, 90.0);
        Assert.InRange(pos.Azimuth, 0.0, 360.0);
    }

    [Fact]
    public void GetSunPosition_Distance_IsReasonable()
    {
        SkyPosition pos = SolarCalculator.GetSunPosition(J2000, Lat, Lon);
        // Earth-Sun distance varies from ~0.983 to ~1.017 AU
        Assert.InRange(pos.Distance, 0.95, 1.05);
    }
}
