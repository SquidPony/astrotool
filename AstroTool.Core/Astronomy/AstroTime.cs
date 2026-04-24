namespace AstroTool.Core.Astronomy;

/// <summary>
/// Time conversions and sidereal time calculations.
/// Based on Jean Meeus, "Astronomical Algorithms" 2nd Ed.
/// </summary>
public static class AstroTime
{
    public const double J2000 = 2451545.0;
    private const double DEG_TO_RAD = Math.PI / 180.0;
    private const double RAD_TO_DEG = 180.0 / Math.PI;

    /// <summary>Convert DateTime (UTC) to Julian Date. Meeus Ch.7</summary>
    public static double ToJulianDate(DateTime dt)
    {
        // Ensure UTC
        if (dt.Kind == DateTimeKind.Local)
            dt = dt.ToUniversalTime();

        int Y = dt.Year;
        int M = dt.Month;
        double D = dt.Day + dt.Hour / 24.0 + dt.Minute / 1440.0 + dt.Second / 86400.0 + dt.Millisecond / 86400000.0;

        if (M <= 2) { Y--; M += 12; }

        int A = (int)(Y / 100.0);
        int B = 2 - A + (int)(A / 4.0);

        return (int)(365.25 * (Y + 4716)) + (int)(30.6001 * (M + 1)) + D + B - 1524.5;
    }

    /// <summary>Convert Julian Date to UTC DateTime. Meeus Ch.7</summary>
    public static DateTime FromJulianDate(double jd)
    {
        double z = Math.Floor(jd + 0.5);
        double f = jd + 0.5 - z;

        double a;
        if (z < 2299161)
        {
            a = z;
        }
        else
        {
            double alpha = Math.Floor((z - 1867216.25) / 36524.25);
            a = z + 1 + alpha - Math.Floor(alpha / 4.0);
        }

        double b = a + 1524;
        double c = Math.Floor((b - 122.1) / 365.25);
        double d = Math.Floor(365.25 * c);
        double e = Math.Floor((b - d) / 30.6001);

        double dayFrac = b - d - Math.Floor(30.6001 * e) + f;
        int month = (int)(e < 14 ? e - 1 : e - 13);
        int year = (int)(month > 2 ? c - 4716 : c - 4715);
        int day = (int)Math.Floor(dayFrac);
        double fracDay = dayFrac - day;

        int hours = (int)(fracDay * 24);
        double fracHour = fracDay * 24 - hours;
        int minutes = (int)(fracHour * 60);
        double fracMin = fracHour * 60 - minutes;
        int seconds = (int)(fracMin * 60);
        int ms = (int)((fracMin * 60 - seconds) * 1000);

        return new DateTime(year, month, day, hours, minutes, seconds, ms, DateTimeKind.Utc);
    }

    /// <summary>T = Julian centuries from J2000.0. Meeus eq. before Ch.22</summary>
    public static double JulianCenturies(double jd) => (jd - J2000) / 36525.0;

    /// <summary>Greenwich Mean Sidereal Time in degrees. Meeus eq. 12.4</summary>
    public static double GreenwichMeanSiderealTime(double jd)
    {
        double T = JulianCenturies(jd);
        // GMST at 0h UT in degrees
        double theta0 = 100.4606184 + 36000.77004 * T + 0.000387933 * T * T - T * T * T / 38710000.0;
        // Add fractional day contribution
        double jd0 = Math.Floor(jd - 0.5) + 0.5; // JD at 0h UT
        double ut = (jd - jd0) * 360.985647;      // degrees
        return Normalize360(theta0 + ut);
    }

    /// <summary>Greenwich Apparent Sidereal Time in degrees. Meeus eq. 12.4 + nutation</summary>
    public static double GreenwichApparentSiderealTime(double jd)
    {
        double T = JulianCenturies(jd);
        double gmst = GreenwichMeanSiderealTime(jd);
        // Nutation in longitude (arcseconds)
        double omega = 125.04452 - 1934.136261 * T;
        double dpsi = -17.20 * Math.Sin(omega * DEG_TO_RAD) - 1.32 * Math.Sin(2 * 280.4665 * DEG_TO_RAD)
                    - 0.23 * Math.Sin(2 * 218.3165 * DEG_TO_RAD) + 0.21 * Math.Sin(2 * omega * DEG_TO_RAD);
        // Mean obliquity
        double epsilon = 23.4392911 - 0.013004167 * T - 1.638889e-7 * T * T + 5.03611e-7 * T * T * T;
        // Equation of the equinoxes
        double eqEq = dpsi * Math.Cos(epsilon * DEG_TO_RAD) / 3600.0;
        return Normalize360(gmst + eqEq);
    }

    /// <summary>Local Apparent Sidereal Time in degrees</summary>
    public static double LocalApparentSiderealTime(double jd, double longitudeDeg)
        => Normalize360(GreenwichApparentSiderealTime(jd) + longitudeDeg);

    /// <summary>Local Mean Sidereal Time in degrees</summary>
    public static double LocalMeanSiderealTime(double jd, double longitudeDeg)
        => Normalize360(GreenwichMeanSiderealTime(jd) + longitudeDeg);

    /// <summary>Normalize angle to [0, 360) degrees</summary>
    public static double Normalize360(double angle)
    {
        angle %= 360.0;
        if (angle < 0) angle += 360.0;
        return angle;
    }

    /// <summary>Normalize angle to [-180, 180) degrees</summary>
    public static double Normalize180(double angle)
    {
        angle = Normalize360(angle);
        if (angle >= 180) angle -= 360;
        return angle;
    }

    /// <summary>Normalize angle to [0, 2π) radians</summary>
    public static double NormalizeTwoPi(double angle)
    {
        angle %= (2 * Math.PI);
        if (angle < 0) angle += 2 * Math.PI;
        return angle;
    }

    /// <summary>
    /// Solve Kepler's equation M = E - e*sin(E) for E (eccentric anomaly).
    /// All angles in radians. Meeus Ch. 30.
    /// </summary>
    public static double SolveKepler(double M, double e, double tolerance = 1e-9)
    {
        M = NormalizeTwoPi(M);
        double E = M; // initial guess
        for (int i = 0; i < 100; i++)
        {
            double dE = (M - E + e * Math.Sin(E)) / (1.0 - e * Math.Cos(E));
            E += dE;
            if (Math.Abs(dE) < tolerance) break;
        }
        return E;
    }

    /// <summary>Convert decimal hours to HH:MM:SS string</summary>
    public static string HoursToHMS(double hours)
    {
        hours = ((hours % 24) + 24) % 24;
        int h = (int)hours;
        double rem = (hours - h) * 60;
        int m = (int)rem;
        int s = (int)((rem - m) * 60);
        return $"{h:D2}h {m:D2}m {s:D2}s";
    }

    /// <summary>Convert decimal degrees to DD°MM'SS" string</summary>
    public static string DegreesToDMS(double degrees)
    {
        string sign = degrees < 0 ? "-" : "+";
        degrees = Math.Abs(degrees);
        int d = (int)degrees;
        double rem = (degrees - d) * 60;
        int m = (int)rem;
        int s = (int)((rem - m) * 60);
        return $"{sign}{d:D2}°{m:D2}'{s:D2}\"";
    }
}
