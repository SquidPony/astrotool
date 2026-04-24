using AstroTool.Core.Astronomy;
using AstroTool.Core.Models;
using Xunit;

namespace AstroTool.Core.Tests;

public class MoonCalculatorTests
{
    private const double Lat = 51.5;
    private const double Lon = 0.0;
    private const double J2000 = 2451545.0;

    [Fact]
    public void GetMoonPosition_ReturnsNonNullResult()
    {
        SkyPosition pos = MoonCalculator.GetMoonPosition(J2000, Lat, Lon);
        Assert.NotNull(pos);
    }

    [Fact]
    public void GetMoonPosition_AltitudeInValidRange()
    {
        SkyPosition pos = MoonCalculator.GetMoonPosition(J2000, Lat, Lon);
        Assert.InRange(pos.Altitude, -90.0, 90.0);
    }

    [Fact]
    public void GetMoonPosition_AzimuthInValidRange()
    {
        SkyPosition pos = MoonCalculator.GetMoonPosition(J2000, Lat, Lon);
        Assert.InRange(pos.Azimuth, 0.0, 360.0);
    }

    [Fact]
    public void GetMoonPosition_RightAscensionInValidRange()
    {
        SkyPosition pos = MoonCalculator.GetMoonPosition(J2000, Lat, Lon);
        Assert.InRange(pos.RightAscension, 0.0, 24.0);
    }

    [Fact]
    public void GetMoonPosition_DeclinationInValidRange()
    {
        SkyPosition pos = MoonCalculator.GetMoonPosition(J2000, Lat, Lon);
        // Moon's declination can reach up to ~28.5° due to orbital inclination
        Assert.InRange(pos.Declination, -30.0, 30.0);
    }

    [Fact]
    public void GetMoonPosition_DistanceInReasonableRange()
    {
        SkyPosition pos = MoonCalculator.GetMoonPosition(J2000, Lat, Lon);
        // Moon distance: perigee ~356,500 km, apogee ~406,700 km
        Assert.InRange(pos.Distance, 350_000.0, 410_000.0);
    }

    [Fact]
    public void GetMoonPosition_IlluminatedFractionInRange()
    {
        SkyPosition pos = MoonCalculator.GetMoonPosition(J2000, Lat, Lon);
        Assert.InRange(pos.IlluminatedFraction, 0.0, 1.0);
    }

    [Fact]
    public void GetMoonPhase_ReturnsValueBetweenZeroAndOne()
    {
        double phase = MoonCalculator.GetMoonPhase(J2000);
        Assert.InRange(phase, 0.0, 1.0);
    }

    [Theory]
    [InlineData(0.03, "New Moon")]
    [InlineData(0.10, "Waxing Crescent")]
    [InlineData(0.25, "First Quarter")]
    [InlineData(0.40, "Waxing Gibbous")]
    [InlineData(0.50, "Full Moon")]
    [InlineData(0.62, "Waning Gibbous")]
    [InlineData(0.75, "Last Quarter")]
    [InlineData(0.85, "Waning Crescent")]
    [InlineData(0.96, "New Moon")]
    public void GetPhaseName_ReturnsCorrectName(double phase, string expectedName)
    {
        string name = MoonCalculator.GetPhaseName(phase);
        Assert.Equal(expectedName, name);
    }

    [Theory]
    [InlineData(60.0, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(-33.9, 151.2)]
    [InlineData(35.7, 139.7)]
    public void GetMoonPosition_DifferentLocations_AltitudeInRange(double lat, double lon)
    {
        SkyPosition pos = MoonCalculator.GetMoonPosition(J2000, lat, lon);
        Assert.InRange(pos.Altitude, -90.0, 90.0);
        Assert.InRange(pos.Azimuth, 0.0, 360.0);
    }
}
