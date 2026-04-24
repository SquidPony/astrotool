using AstroTool.Core.Astronomy;
using Xunit;

namespace AstroTool.Core.Tests;

public class CoordinateConverterTests
{
    private const double Tolerance = 0.01; // degrees

    [Fact]
    public void ObliquityOfEcliptic_AtJ2000_IsApproximately23_44()
    {
        double T = 0.0; // J2000
        double eps = CoordinateConverter.ObliquityOfEcliptic(T);
        // IAU value at J2000: ~23.439291°
        Assert.InRange(eps, 23.43, 23.45);
    }

    [Fact]
    public void EquatorialToHorizontal_ObjectOnMeridian_CorrectAltitude()
    {
        // Object exactly on meridian at latitude 45° with dec = 45°
        // Altitude = 90° - |lat - dec| = 90° - 0° = 90° ... wait, that's only at equator.
        // For lat=45, dec=45, HA=0: alt = arcsin(sin(45)*sin(45) + cos(45)*cos(45)*1) = arcsin(1) = 90°
        double lat = 45.0;
        double dec = 45.0;
        double lst = 12.0 * 15.0; // LST in degrees
        double ra = 12.0;         // RA in hours → on meridian
        var (alt, az) = CoordinateConverter.EquatorialToHorizontal(ra, dec, lst, lat);
        Assert.Equal(90.0, alt, precision: 6);
    }

    [Fact]
    public void EquatorialToHorizontal_ObjectAtHorizon_AltitudeNearZero()
    {
        // At equator (lat=0), an object with dec=0 on the horizon (HA=90°)
        double lat = 0.0;
        double dec = 0.0;
        double ra = 0.0;
        double lst = 6.0 * 15.0; // HA = 90°
        var (alt, _) = CoordinateConverter.EquatorialToHorizontal(ra, dec, lst, lat);
        Assert.Equal(0.0, alt, precision: 6);
    }

    [Fact]
    public void EclipticToEquatorial_VernalEquinox_RaZero()
    {
        // At vernal equinox: lambda=0, beta=0, epsilon=23.44° → RA=0h, Dec=0°
        double epsilon = 23.44;
        var (ra, dec) = CoordinateConverter.EclipticToEquatorial(0.0, 0.0, epsilon);
        Assert.Equal(0.0, ra, Tolerance);
        Assert.Equal(0.0, dec, Tolerance);
    }

    [Fact]
    public void EclipticToEquatorial_SummerSolstice_CorrectDec()
    {
        // At summer solstice: lambda=90°, beta=0, → Dec = obliquity ≈ 23.44°
        double epsilon = 23.44;
        var (_, dec) = CoordinateConverter.EclipticToEquatorial(90.0, 0.0, epsilon);
        Assert.InRange(dec, 23.0, 24.0);
    }

    [Fact]
    public void EquatorialToEcliptic_RoundTrip_PreservesCoordinates()
    {
        double epsilon = CoordinateConverter.ObliquityOfEcliptic(0.0);
        double originalRa = 6.0;    // hours
        double originalDec = 23.44; // degrees

        var (lambda, beta) = CoordinateConverter.EquatorialToEcliptic(originalRa, originalDec, epsilon);
        var (ra, dec) = CoordinateConverter.EclipticToEquatorial(lambda, beta, epsilon);

        Assert.Equal(originalRa, ra, Tolerance);
        Assert.Equal(originalDec, dec, Tolerance);
    }

    [Fact]
    public void HorizontalToEquatorial_RoundTrip_PreservesCoordinates()
    {
        double lat = 48.0;
        double lst = 120.0; // degrees
        double originalRa = 6.0;
        double originalDec = 30.0;

        var (alt, az) = CoordinateConverter.EquatorialToHorizontal(originalRa, originalDec, lst, lat);
        var (ra, dec) = CoordinateConverter.HorizontalToEquatorial(alt, az, lst, lat);

        Assert.Equal(originalRa, ra, Tolerance);
        Assert.Equal(originalDec, dec, Tolerance);
    }

    [Fact]
    public void PrecessJ2000_ZeroCenturies_ReturnsOriginalCoordinates()
    {
        double ra = 6.75;
        double dec = 16.71;
        var (precRa, precDec) = CoordinateConverter.PrecessJ2000(ra, dec, 0.0);
        // At T=0 no precession should occur
        Assert.Equal(ra, precRa, Tolerance);
        Assert.Equal(dec, precDec, Tolerance);
    }

    [Fact]
    public void ApparentObliquity_AtJ2000_IsNearMeanObliquity()
    {
        double T = 0.0;
        double mean = CoordinateConverter.ObliquityOfEcliptic(T);
        double apparent = CoordinateConverter.ApparentObliquity(T);
        // Nutation correction is small (~0.003°)
        Assert.Equal(mean, apparent, 1);
    }
}
