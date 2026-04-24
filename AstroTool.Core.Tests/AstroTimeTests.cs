using AstroTool.Core.Astronomy;
using Xunit;

namespace AstroTool.Core.Tests;

public class AstroTimeTests
{
    // J2000.0 epoch: 2000 January 1.5 TT = JD 2451545.0 (Meeus p.61)
    private const double J2000 = 2451545.0;
    private const double Epsilon = 1e-6;

    [Fact]
    public void ToJulianDate_J2000Epoch_Returns2451545()
    {
        var dt = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        double jd = AstroTime.ToJulianDate(dt);
        Assert.Equal(J2000, jd, precision: 5);
    }

    [Theory]
    [InlineData(1987, 4, 10, 0, 0, 0, 2446895.5)]   // Meeus Example 7.a
    [InlineData(1988, 6, 19, 12, 0, 0, 2447332.0)]  // Meeus Example 7.b
    [InlineData(1900, 1, 1, 0, 0, 0, 2415020.5)]
    public void ToJulianDate_KnownDates_ReturnsCorrectJD(
        int year, int month, int day, int hour, int minute, int second, double expectedJd)
    {
        var dt = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
        double jd = AstroTime.ToJulianDate(dt);
        Assert.Equal(expectedJd, jd, precision: 4);
    }

    [Theory]
    [InlineData(2451545.0, 2000, 1, 1, 12)]   // J2000.0
    [InlineData(2446895.5, 1987, 4, 10, 0)]   // Meeus 7.a
    public void FromJulianDate_KnownJDs_ReturnsCorrectDate(
        double jd, int expectedYear, int expectedMonth, int expectedDay, int expectedHour)
    {
        DateTime dt = AstroTime.FromJulianDate(jd);
        Assert.Equal(expectedYear, dt.Year);
        Assert.Equal(expectedMonth, dt.Month);
        Assert.Equal(expectedDay, dt.Day);
        Assert.Equal(expectedHour, dt.Hour);
    }

    [Fact]
    public void JulianDate_RoundTrip_PreservesDateTime()
    {
        var original = new DateTime(2023, 6, 15, 18, 30, 45, DateTimeKind.Utc);
        double jd = AstroTime.ToJulianDate(original);
        DateTime recovered = AstroTime.FromJulianDate(jd);

        Assert.Equal(original.Year, recovered.Year);
        Assert.Equal(original.Month, recovered.Month);
        Assert.Equal(original.Day, recovered.Day);
        Assert.Equal(original.Hour, recovered.Hour);
        Assert.Equal(original.Minute, recovered.Minute);
        Assert.Equal(original.Second, recovered.Second);
    }

    [Fact]
    public void JulianCenturies_AtJ2000_ReturnsZero()
    {
        double T = AstroTime.JulianCenturies(J2000);
        Assert.Equal(0.0, T, precision: 10);
    }

    [Fact]
    public void JulianCenturies_OneHundredYearsAfterJ2000_ReturnsOne()
    {
        double T = AstroTime.JulianCenturies(J2000 + 36525.0);
        Assert.Equal(1.0, T, precision: 10);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(360.0, 0.0)]
    [InlineData(720.0, 0.0)]
    [InlineData(-90.0, 270.0)]
    [InlineData(450.0, 90.0)]
    [InlineData(359.9, 359.9)]
    public void Normalize360_ReturnsAngleInRange(double input, double expected)
    {
        double result = AstroTime.Normalize360(input);
        Assert.Equal(expected, result, precision: 6);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(180.0, -180.0)]
    [InlineData(270.0, -90.0)]
    [InlineData(90.0, 90.0)]
    [InlineData(-90.0, -90.0)]
    public void Normalize180_ReturnsAngleInRange(double input, double expected)
    {
        double result = AstroTime.Normalize180(input);
        Assert.Equal(expected, result, precision: 6);
    }

    [Theory]
    [InlineData(0.0, 0.0, 0.0)]        // circular orbit: M = E
    [InlineData(Math.PI / 2, 0.0, Math.PI / 2)] // M = π/2, e = 0 → E = π/2
    public void SolveKepler_ZeroEccentricity_EEqualsM(double M, double e, double expectedE)
    {
        double E = AstroTime.SolveKepler(M, e);
        Assert.Equal(expectedE, E, precision: 9);
    }

    [Fact]
    public void SolveKepler_SatisfiesKeplersEquation()
    {
        double M = 1.0;   // radians
        double e = 0.5;
        double E = AstroTime.SolveKepler(M, e);
        // Verify M = E - e*sin(E)
        double Mcomputed = E - e * Math.Sin(E);
        Assert.Equal(M, Mcomputed, precision: 8);
    }

    [Fact]
    public void GreenwichMeanSiderealTime_ReturnsValueInZeroTo360()
    {
        double gmst = AstroTime.GreenwichMeanSiderealTime(J2000);
        Assert.InRange(gmst, 0.0, 360.0);
    }

    [Fact]
    public void HoursToHMS_ReturnsCorrectFormat()
    {
        string result = AstroTime.HoursToHMS(12.5);
        Assert.Equal("12h 30m 00s", result);
    }

    [Fact]
    public void DegreesToDMS_PositiveDegrees_ReturnsCorrectFormat()
    {
        string result = AstroTime.DegreesToDMS(45.5);
        Assert.Equal("+45°30'00\"", result);
    }

    [Fact]
    public void DegreesToDMS_NegativeDegrees_ReturnsNegativeSign()
    {
        string result = AstroTime.DegreesToDMS(-30.25);
        Assert.StartsWith("-", result);
    }
}
