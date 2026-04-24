using AstroTool.Core.Models;

namespace AstroTool.Core.Astronomy;

/// <summary>
/// Sun position using VSOP87 simplified series (Meeus Ch. 25).
/// Accuracy: ~0.01° for years 1950-2050.
/// </summary>
public static class SolarCalculator
{
    private const double DEG = Math.PI / 180.0;
    private const double RAD = 180.0 / Math.PI;

    /// <summary>
    /// Calculate the Sun's position. Meeus Chapter 25.
    /// Returns geocentric equatorial coordinates and horizontal coords.
    /// </summary>
    public static SkyPosition GetSunPosition(double jd, double latitudeDeg, double longitudeDeg)
    {
        double T = AstroTime.JulianCenturies(jd);

        // Geometric mean longitude of the Sun (degrees). Meeus Eq. 25.2
        double L0 = AstroTime.Normalize360(280.46646 + 36000.76983 * T + 0.0003032 * T * T);

        // Mean anomaly of the Sun (degrees). Meeus Eq. 25.3
        double M = AstroTime.Normalize360(357.52911 + 35999.05029 * T - 0.0001537 * T * T);
        double Mrad = M * DEG;

        // Equation of center C. Meeus Eq. 25.4
        double C = (1.914602 - 0.004817 * T - 0.000014 * T * T) * Math.Sin(Mrad)
                 + (0.019993 - 0.000101 * T) * Math.Sin(2 * Mrad)
                 + 0.000289 * Math.Sin(3 * Mrad);

        // Sun's true longitude
        double sunLon = L0 + C;

        // Sun's true anomaly
        double v = M + C;

        // Sun's radius vector (distance in AU). Meeus Eq. 25.5
        double R = 1.000001018 * (1 - 0.016708634 * 0.016708634 - 0.016708634 * 0.000042037 * T)
                 / (1 + 0.016708634 * Math.Cos(v * DEG));
        // More precise version
        R = (1.000001018 * (1.0 - 0.016708634 * 0.016708634))
          / (1.0 + 0.016708634 * Math.Cos(v * DEG));

        // Apparent longitude (correct for aberration & nutation). Meeus Eq. 25.10
        double omega = AstroTime.Normalize360(125.04 - 1934.136 * T);
        double lambda = sunLon - 0.00569 - 0.00478 * Math.Sin(omega * DEG);

        // Apparent obliquity
        double epsilon = CoordinateConverter.ApparentObliquity(T);

        // Convert to equatorial. Meeus Ch. 25.
        var (ra, dec) = CoordinateConverter.EclipticToEquatorial(lambda, 0.0, epsilon);

        // Get LST
        double lst = AstroTime.LocalApparentSiderealTime(jd, longitudeDeg);
        var (alt, az) = CoordinateConverter.EquatorialToHorizontal(ra, dec, lst, latitudeDeg);

        // Heliocentric XY for solar system view (Sun is at origin)
        double xH = 0, yH = 0;

        return new SkyPosition
        {
            RightAscension = ra,
            Declination = dec,
            Altitude = alt,
            Azimuth = az,
            Distance = R,
            EclipticLongitude = lambda,
            EclipticLatitude = 0.0,
            X = xH,
            Y = yH,
            Magnitude = -26.74, // apparent magnitude
            AngularDiameter = 1919.26 / R // arcseconds
        };
    }

    /// <summary>
    /// Equation of Time in minutes. Meeus Ch. 27.
    /// Positive = sun is ahead of mean sun.
    /// </summary>
    public static double EquationOfTime(double jd)
    {
        double T = AstroTime.JulianCenturies(jd);
        double L0 = AstroTime.Normalize360(280.46646 + 36000.76983 * T) * DEG;
        double e = 0.016708634 - 0.000042037 * T;
        double M = AstroTime.Normalize360(357.52911 + 35999.05029 * T) * DEG;
        double eps = CoordinateConverter.ObliquityOfEcliptic(T) * DEG;
        double y = Math.Tan(eps / 2.0);
        y *= y;

        double E = y * Math.Sin(2 * L0)
                 - 2 * e * Math.Sin(M)
                 + 4 * e * y * Math.Sin(M) * Math.Cos(2 * L0)
                 - 0.5 * y * y * Math.Sin(4 * L0)
                 - 1.25 * e * e * Math.Sin(2 * M);

        return E * 4.0 * RAD; // in minutes
    }

    /// <summary>
    /// Get ecliptic coordinates of the Sun's center.
    /// Returns (longitude degrees, latitude degrees, distance AU).
    /// </summary>
    public static (double Longitude, double Latitude, double Distance) GetEclipticCoordinates(double jd)
    {
        double T = AstroTime.JulianCenturies(jd);
        double L0 = AstroTime.Normalize360(280.46646 + 36000.76983 * T + 0.0003032 * T * T);
        double M = AstroTime.Normalize360(357.52911 + 35999.05029 * T - 0.0001537 * T * T);
        double Mrad = M * DEG;
        double C = (1.914602 - 0.004817 * T - 0.000014 * T * T) * Math.Sin(Mrad)
                 + (0.019993 - 0.000101 * T) * Math.Sin(2 * Mrad)
                 + 0.000289 * Math.Sin(3 * Mrad);
        double sunLon = L0 + C;
        double v = M + C;
        double R = (1.000001018 * (1.0 - 0.016708634 * 0.016708634))
                 / (1.0 + 0.016708634 * Math.Cos(v * DEG));
        double omega = 125.04 - 1934.136 * T;
        double lambda = sunLon - 0.00569 - 0.00478 * Math.Sin(omega * DEG);
        return (AstroTime.Normalize360(lambda), 0.0, R);
    }
}
