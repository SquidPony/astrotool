namespace AstroTool.Core.Astronomy;

/// <summary>
/// Coordinate system conversions for astronomical positions.
/// Jean Meeus, "Astronomical Algorithms" 2nd Ed.
/// All angles in DEGREES unless otherwise stated.
/// </summary>
public static class CoordinateConverter
{
    private const double DEG = Math.PI / 180.0;
    private const double RAD = 180.0 / Math.PI;

    /// <summary>
    /// Mean obliquity of the ecliptic. Meeus Eq. 22.2.
    /// T = Julian centuries from J2000.
    /// Returns degrees.
    /// </summary>
    public static double ObliquityOfEcliptic(double T)
    {
        // Meeus Eq. 22.2 — IAU formula, accurate to 0.01" over ±1000 years
        double eps0 = 23.0 + 26.0 / 60.0 + 21.448 / 3600.0
                    - (46.8150 / 3600.0) * T
                    - (0.00059 / 3600.0) * T * T
                    + (0.001813 / 3600.0) * T * T * T;
        return eps0;
    }

    /// <summary>
    /// Apparent obliquity (includes nutation). Meeus Eq. 22.2.
    /// </summary>
    public static double ApparentObliquity(double T)
    {
        double omega = 125.04452 - 1934.136261 * T; // degrees
        double epsilon0 = ObliquityOfEcliptic(T);
        return epsilon0 + 0.00256 * Math.Cos(omega * DEG); // Meeus correction
    }

    /// <summary>
    /// Convert equatorial (RA, Dec) to horizontal (Altitude, Azimuth).
    /// Meeus Ch. 13.
    /// ra in hours, dec in degrees, lst in degrees, lat in degrees.
    /// Returns (altitude degrees, azimuth degrees 0=N, clockwise).
    /// </summary>
    public static (double Altitude, double Azimuth) EquatorialToHorizontal(
        double raHours, double decDeg, double lstDeg, double latitudeDeg)
    {
        double raDeg = raHours * 15.0;
        double H = (lstDeg - raDeg) * DEG; // Hour angle in radians
        double dec = decDeg * DEG;
        double lat = latitudeDeg * DEG;

        double sinAlt = Math.Sin(dec) * Math.Sin(lat) + Math.Cos(dec) * Math.Cos(lat) * Math.Cos(H);
        sinAlt = Math.Max(-1.0, Math.Min(1.0, sinAlt));
        double altitude = Math.Asin(sinAlt) * RAD;

        double cosAlt = Math.Cos(altitude * DEG);
        if (Math.Abs(cosAlt) < 1e-12)
        {
            // Azimuth is undefined at zenith/nadir; return canonical North.
            return (altitude, 0.0);
        }

        double cosAz = (Math.Sin(dec) - Math.Sin(lat) * sinAlt) / (Math.Cos(lat) * cosAlt);
        // Clamp due to floating point
        cosAz = Math.Max(-1.0, Math.Min(1.0, cosAz));
        double azimuth = Math.Acos(cosAz) * RAD;

        // Azimuth measured from North clockwise
        if (Math.Sin(H) > 0) azimuth = 360.0 - azimuth;
        return (altitude, azimuth);
    }

    /// <summary>
    /// Convert horizontal (altitude, azimuth) to equatorial (RA, Dec).
    /// Meeus Ch. 13.
    /// Returns (RA in hours, Dec in degrees).
    /// </summary>
    public static (double RA, double Dec) HorizontalToEquatorial(
        double altitudeDeg, double azimuthDeg, double lstDeg, double latitudeDeg)
    {
        double alt = altitudeDeg * DEG;
        double az = azimuthDeg * DEG;
        double lat = latitudeDeg * DEG;

        double sinDec = Math.Sin(alt) * Math.Sin(lat) + Math.Cos(alt) * Math.Cos(lat) * Math.Cos(az);
        double dec = Math.Asin(sinDec) * RAD;

        double cosH = (Math.Sin(alt) - Math.Sin(lat) * sinDec) / (Math.Cos(lat) * Math.Cos(dec * DEG));
        cosH = Math.Max(-1.0, Math.Min(1.0, cosH));
        double H = Math.Acos(cosH) * RAD;
        if (Math.Sin(az) > 0) H = 360.0 - H;

        double ra = AstroTime.Normalize360(lstDeg - H) / 15.0;
        return (ra, dec);
    }

    /// <summary>
    /// Convert ecliptic (lambda, beta) to equatorial (RA, Dec).
    /// Meeus Ch. 13.
    /// lambda = ecliptic longitude degrees, beta = ecliptic latitude degrees,
    /// epsilon = obliquity of the ecliptic degrees.
    /// Returns (RA hours, Dec degrees).
    /// </summary>
    public static (double RA, double Dec) EclipticToEquatorial(
        double lambdaDeg, double betaDeg, double epsilonDeg)
    {
        double lam = lambdaDeg * DEG;
        double bet = betaDeg * DEG;
        double eps = epsilonDeg * DEG;

        double sinDec = Math.Sin(bet) * Math.Cos(eps) + Math.Cos(bet) * Math.Sin(eps) * Math.Sin(lam);
        double dec = Math.Asin(sinDec) * RAD;

        double y = Math.Sin(lam) * Math.Cos(eps) - Math.Tan(bet) * Math.Sin(eps);
        double x = Math.Cos(lam);
        double ra = Math.Atan2(y, x) * RAD;
        ra = AstroTime.Normalize360(ra) / 15.0; // convert to hours
        return (ra, dec);
    }

    /// <summary>
    /// Convert equatorial (RA, Dec) to ecliptic (lambda, beta).
    /// Returns (lambda degrees, beta degrees).
    /// </summary>
    public static (double Lambda, double Beta) EquatorialToEcliptic(
        double raHours, double decDeg, double epsilonDeg)
    {
        double ra = raHours * 15.0 * DEG;
        double dec = decDeg * DEG;
        double eps = epsilonDeg * DEG;

        double sinBet = Math.Sin(dec) * Math.Cos(eps) - Math.Cos(dec) * Math.Sin(eps) * Math.Sin(ra);
        double beta = Math.Asin(sinBet) * RAD;

        double y = Math.Sin(ra) * Math.Cos(eps) + Math.Tan(dec) * Math.Sin(eps);
        double x = Math.Cos(ra);
        double lambda = Math.Atan2(y, x) * RAD;
        return (AstroTime.Normalize360(lambda), beta);
    }

    /// <summary>
    /// Precess equatorial coordinates from J2000.0 to given epoch.
    /// Meeus Ch. 21, simplified Rigorous Method.
    /// T is Julian centuries of the target epoch.
    /// Returns (RA hours, Dec degrees).
    /// </summary>
    public static (double RA, double Dec) PrecessJ2000(double ra2000Hours, double dec2000Deg, double T)
    {
        double ra0 = ra2000Hours * 15.0 * DEG; // radians
        double dec0 = dec2000Deg * DEG;

        // Precessional constants in arcseconds (Meeus Eq.21.2)
        double zeta = (2306.2181 + 1.39656 * 0 - 0.000139 * 0) * T
                    + (0.30188 - 0.000344 * 0) * T * T + 0.017998 * T * T * T;
        double z = (2306.2181 + 1.39656 * 0 - 0.000139 * 0) * T
                 + (1.09468 + 0.000066 * 0) * T * T + 0.018203 * T * T * T;
        double theta = (2004.3109 - 0.85330 * 0 - 0.000217 * 0) * T
                     - (0.42665 + 0.000217 * 0) * T * T - 0.041775 * T * T * T;

        zeta *= DEG / 3600.0;
        z *= DEG / 3600.0;
        theta *= DEG / 3600.0;

        double A = Math.Cos(dec0) * Math.Sin(ra0 + zeta);
        double B = Math.Cos(theta) * Math.Cos(dec0) * Math.Cos(ra0 + zeta) - Math.Sin(theta) * Math.Sin(dec0);
        double C = Math.Sin(theta) * Math.Cos(dec0) * Math.Cos(ra0 + zeta) + Math.Cos(theta) * Math.Sin(dec0);

        double raN = Math.Atan2(A, B) + z;
        double decN = Math.Asin(C);

        return (AstroTime.Normalize360(raN * RAD) / 15.0, decN * RAD);
    }
}
