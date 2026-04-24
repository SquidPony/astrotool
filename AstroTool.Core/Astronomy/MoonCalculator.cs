using AstroTool.Core.Models;

namespace AstroTool.Core.Astronomy;

/// <summary>
/// Moon position using ELP2000 simplified series.
/// Meeus Chapter 47 (low-accuracy theory) — accuracy ~10" in longitude, ~4" in latitude.
/// </summary>
public static class MoonCalculator
{
    private const double DEG = Math.PI / 180.0;
    private const double RAD = 180.0 / Math.PI;

    // Longitude perturbation terms (Meeus Table 47.A) — coefficient in 0.001"
    // Each row: [D, M, M', F, longitude_coeff, distance_coeff]
    private static readonly double[,] LonDistTerms = {
        {  0,  0,  1,  0,  6288774, -20905355 },
        {  2,  0, -1,  0,  1274027,  -3699111 },
        {  2,  0,  0,  0,   658314,  -2955968 },
        {  0,  0,  2,  0,   213618,   -569925 },
        {  0,  1,  0,  0,  -185116,    48888 },
        {  0,  0,  0,  2,  -114332,    -3149 },
        {  2,  0, -2,  0,    58793,   246158 },
        {  2, -1, -1,  0,    57066,  -152138 },
        {  2,  0,  1,  0,    53322,  -170733 },
        {  2, -1,  0,  0,    45758,  -204586 },
        {  0,  1, -1,  0,   -40923,   -129620 },
        {  1,  0,  0,  0,   -34720,   108743 },
        {  0,  1,  1,  0,   -30383,   104755 },
        {  2,  0,  0, -2,    15327,    10321 },
        {  0,  0,  1,  2,   -12528,        0 },
        {  0,  0,  1, -2,    10980,    79661 },
        {  4,  0, -1,  0,    10675,   -34782 },
        {  0,  0,  3,  0,    10034,   -23210 },
        {  4,  0, -2,  0,     8548,   -21636 },
        {  2,  1, -1,  0,    -7888,    24208 },
        {  2,  1,  0,  0,    -6766,    30824 },
        {  1,  0, -1,  0,    -5163,    -8379 },
        {  1,  1,  0,  0,     4987,   -16675 },
        {  2, -1,  1,  0,     4036,   -12831 },
        {  2,  0,  2,  0,     3994,   -10445 },
        {  4,  0,  0,  0,     3861,   -11650 },
        {  2,  0, -3,  0,     3665,    14403 },
        {  0,  1, -2,  0,    -2689,    -7003 },
        {  2,  0, -1,  2,    -2602,        0 },
        {  2, -1, -2,  0,     2390,    10056 },
        {  1,  0,  1,  0,    -2348,     6322 },
        {  2, -2,  0,  0,     2236,    -9884 },
        {  0,  1,  2,  0,    -2120,     5751 },
        {  0,  2,  0,  0,    -2069,        0 },
        {  2, -2, -1,  0,     2048,    -4950 },
        {  2,  0,  1, -2,    -1773,     4130 },
        {  2,  0,  0,  2,    -1595,        0 },
        {  4, -1, -1,  0,     1215,    -3958 },
        {  0,  0,  2,  2,    -1110,        0 },
        {  3,  0, -1,  0,     -892,     3258 },
        {  2,  1,  1,  0,     -810,     2616 },
        {  4, -1, -2,  0,      759,    -1897 },
        {  0,  2, -1,  0,     -713,    -2117 },
        {  2,  2, -1,  0,     -700,     2354 },
        {  2,  1, -2,  0,      691,        0 },
        {  2, -1,  0, -2,      596,        0 },
        {  4,  0,  1,  0,      549,    -1423 },
        {  0,  0,  4,  0,      537,    -1117 },
        {  4, -1,  0,  0,      520,    -1571 },
        {  1,  0, -2,  0,     -487,    -1739 },
        {  2,  1,  0, -2,     -399,        0 },
        {  0,  0,  2, -2,     -381,    -4421 },
        {  1,  1,  1,  0,      351,        0 },
        {  3,  0, -2,  0,     -340,        0 },
        {  4,  0, -3,  0,      330,        0 },
        {  2, -1,  2,  0,      327,        0 },
        {  0,  2,  1,  0,     -323,     1165 },
        {  1,  1, -1,  0,      299,        0 },
        {  2,  0,  3,  0,      294,        0 },
        {  2,  0, -1, -2,        0,     8752 },
    };

    // Latitude terms (Meeus Table 47.B)
    private static readonly double[,] LatTerms = {
        {  0,  0,  0,  1,  5128122 },
        {  0,  0,  1,  1,   280602 },
        {  0,  0,  1, -1,   277693 },
        {  2,  0,  0, -1,   173237 },
        {  2,  0, -1,  1,    55413 },
        {  2,  0, -1, -1,    46271 },
        {  2,  0,  0,  1,    32573 },
        {  0,  0,  2,  1,    17198 },
        {  2,  0,  1, -1,     9266 },
        {  0,  0,  2, -1,     8822 },
        {  2, -1,  0, -1,     8216 },
        {  2,  0, -2, -1,     4324 },
        {  2,  0,  1,  1,     4200 },
        {  2,  1,  0, -1,    -3359 },
        {  2, -1, -1,  1,     2463 },
        {  2, -1,  0,  1,     2211 },
        {  2, -1, -1, -1,     2065 },
        {  0,  1, -1, -1,    -1870 },
        {  4,  0, -1, -1,     1828 },
        {  0,  1,  0,  1,    -1794 },
        {  0,  0,  0,  3,    -1749 },
        {  0,  1, -1,  1,    -1565 },
        {  1,  0,  0,  1,    -1491 },
        {  0,  1,  1,  1,    -1475 },
        {  0,  1,  1, -1,    -1410 },
        {  0,  1,  0, -1,    -1344 },
        {  1,  0,  0, -1,    -1335 },
        {  0,  0,  3,  1,     1107 },
        {  4,  0,  0, -1,     1021 },
        {  4,  0, -1,  1,      833 },
    };

    /// <summary>
    /// Calculate the Moon's position. Meeus Chapter 47.
    /// Returns SkyPosition with full coordinates.
    /// </summary>
    public static SkyPosition GetMoonPosition(double jd, double latitudeDeg, double longitudeDeg)
    {
        double T = AstroTime.JulianCenturies(jd);
        double T2 = T * T;
        double T3 = T2 * T;
        double T4 = T3 * T;

        // Moon's mean longitude. Meeus Eq. 47.1
        double Lprime = AstroTime.Normalize360(
            218.3164477 + 481267.88123421 * T - 0.0015786 * T2 + T3 / 538841.0 - T4 / 65194000.0);

        // Moon's mean anomaly M'. Meeus Eq. 47.4
        double Mprime = AstroTime.Normalize360(
            134.9633964 + 477198.8676313 * T + 0.0089970 * T2 + T3 / 69699.0 - T4 / 14712000.0);

        // Sun's mean anomaly M. Meeus Eq. 47.3 (reuse from solar)
        double M = AstroTime.Normalize360(
            357.5291092 + 35999.0502909 * T - 0.0001536 * T2 + T3 / 24490000.0);

        // Moon's argument of latitude F. Meeus Eq. 47.5
        double F = AstroTime.Normalize360(
            93.2720950 + 483202.0175233 * T - 0.0036539 * T2 - T3 / 3526000.0 + T4 / 863310000.0);

        // Elongation of the Moon. Meeus Eq. 47.2
        double D = AstroTime.Normalize360(
            297.8501921 + 445267.1114034 * T - 0.0018819 * T2 + T3 / 545868.0 - T4 / 113065000.0);

        // Additional arguments for periodic terms
        double A1 = AstroTime.Normalize360(119.75 + 131.849 * T);
        double A2 = AstroTime.Normalize360(53.09 + 479264.290 * T);
        double A3 = AstroTime.Normalize360(313.45 + 481266.484 * T);

        // Eccentricity of Earth's orbit. Meeus Eq. 47.6
        double E = 1.0 - 0.002516 * T - 0.0000074 * T2;
        double E2 = E * E;

        double Drad = D * DEG;
        double Mrad = M * DEG;
        double Mprad = Mprime * DEG;
        double Frad = F * DEG;

        // Accumulate longitude and distance perturbations
        double sumL = 0, sumR = 0;
        int nTerms = LonDistTerms.GetLength(0);
        for (int i = 0; i < nTerms; i++)
        {
            double d = LonDistTerms[i, 0];
            double m = LonDistTerms[i, 1];
            double mp = LonDistTerms[i, 2];
            double f = LonDistTerms[i, 3];
            double lCoeff = LonDistTerms[i, 4];
            double rCoeff = LonDistTerms[i, 5];

            double eCorr = 1.0;
            if (Math.Abs(m) == 1) eCorr = E;
            else if (Math.Abs(m) == 2) eCorr = E2;

            double arg = d * Drad + m * Mrad + mp * Mprad + f * Frad;
            sumL += eCorr * lCoeff * Math.Sin(arg);
            sumR += eCorr * rCoeff * Math.Cos(arg);
        }

        // Additional longitude terms (Meeus p.338)
        sumL += 3958 * Math.Sin(A1 * DEG)
              + 1962 * Math.Sin((Lprime - F) * DEG)
              + 318 * Math.Sin(A2 * DEG);

        // Accumulate latitude perturbations
        double sumB = 0;
        int nLat = LatTerms.GetLength(0);
        for (int i = 0; i < nLat; i++)
        {
            double d = LatTerms[i, 0];
            double m = LatTerms[i, 1];
            double mp = LatTerms[i, 2];
            double f = LatTerms[i, 3];
            double bCoeff = LatTerms[i, 4];

            double eCorr = 1.0;
            if (Math.Abs(m) == 1) eCorr = E;
            else if (Math.Abs(m) == 2) eCorr = E2;

            double arg = d * Drad + m * Mrad + mp * Mprad + f * Frad;
            sumB += eCorr * bCoeff * Math.Sin(arg);
        }

        // Additional latitude terms (Meeus p.338)
        sumB -= 2235 * Math.Sin(Lprime * DEG)
              + 382 * Math.Sin(A3 * DEG)
              + 175 * Math.Sin((A1 - F) * DEG)
              + 175 * Math.Sin((A1 + F) * DEG)
              + 127 * Math.Sin((Lprime - Mprime) * DEG)
              - 115 * Math.Sin((Lprime + Mprime) * DEG);

        // Moon's true longitude and latitude in degrees
        double lambda = AstroTime.Normalize360(Lprime + sumL / 1e6);
        double beta = sumB / 1e6;

        // Distance in km. Meeus Eq. 47.12
        double distKm = 385000.56 + sumR / 1000.0;

        // Nutation & apparent longitude
        double omega = AstroTime.Normalize360(125.04452 - 1934.136261 * T);
        double dpsi = (-17.20 * Math.Sin(omega * DEG) - 1.32 * Math.Sin(2 * 280.4665 * DEG)) / 3600.0;
        double apparentLambda = lambda + dpsi;

        // Obliquity
        double epsilon = CoordinateConverter.ApparentObliquity(T);

        // Equatorial coordinates
        var (ra, dec) = CoordinateConverter.EclipticToEquatorial(apparentLambda, beta, epsilon);

        // Horizontal coordinates
        double lst = AstroTime.LocalApparentSiderealTime(jd, longitudeDeg);
        var (alt, az) = CoordinateConverter.EquatorialToHorizontal(ra, dec, lst, latitudeDeg);

        // Phase angle
        var (sunLon, _, sunDist) = SolarCalculator.GetEclipticCoordinates(jd);
        double elongation = AstroTime.Normalize360(lambda - sunLon);
        double psi = Math.Acos(Math.Cos(beta * DEG) * Math.Cos(elongation * DEG)) * RAD;
        double phaseAngle = Math.Atan2(sunDist * Math.Sin(psi * DEG),
                                        distKm / 149597870.7 - sunDist * Math.Cos(psi * DEG)) * RAD;
        double illum = (1.0 + Math.Cos(phaseAngle * DEG)) / 2.0;

        // Apparent magnitude (Meeus p. 342)
        double mag = -12.73 + 0.026 * Math.Abs(phaseAngle) + 4e-9 * Math.Pow(phaseAngle, 4);

        // Equatorial horizontal parallax (Meeus Eq. 47.15)
        double parallax = Math.Asin(6378.14 / distKm) * RAD;

        return new SkyPosition
        {
            RightAscension = ra,
            Declination = dec,
            Altitude = alt,
            Azimuth = az,
            Distance = distKm,
            EclipticLongitude = lambda,
            EclipticLatitude = beta,
            Magnitude = mag,
            PhaseAngle = phaseAngle,
            IlluminatedFraction = illum,
            AngularDiameter = 2.0 * parallax * 60.0, // arc-minutes, rough
            X = distKm * Math.Cos(beta * DEG) * Math.Cos(lambda * DEG) / 149597870.7,
            Y = distKm * Math.Cos(beta * DEG) * Math.Sin(lambda * DEG) / 149597870.7
        };
    }

    /// <summary>
    /// Compute Moon phase: 0=new, 0.25=first quarter, 0.5=full, 0.75=last quarter.
    /// </summary>
    public static double GetMoonPhase(double jd)
    {
        double T = AstroTime.JulianCenturies(jd);
        double Lprime = AstroTime.Normalize360(218.3164477 + 481267.88123421 * T);
        double sunLon = SolarCalculator.GetEclipticCoordinates(jd).Longitude;
        double elongation = AstroTime.Normalize360(Lprime - sunLon);
        return elongation / 360.0;
    }

    /// <summary>
    /// Get human-readable phase name.
    /// </summary>
    public static string GetPhaseName(double phase)
    {
        if (phase < 0.0625) return "New Moon";
        if (phase < 0.1875) return "Waxing Crescent";
        if (phase < 0.3125) return "First Quarter";
        if (phase < 0.4375) return "Waxing Gibbous";
        if (phase < 0.5625) return "Full Moon";
        if (phase < 0.6875) return "Waning Gibbous";
        if (phase < 0.8125) return "Last Quarter";
        if (phase < 0.9375) return "Waning Crescent";
        return "New Moon";
    }
}
