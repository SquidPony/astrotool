using AstroTool.Core.Models;

namespace AstroTool.Core.Astronomy;

/// <summary>
/// Planetary positions using VSOP87 low-precision theory.
/// Based on Meeus Chapters 31-32 and Table 31.a.
/// Uses mean orbital elements with secular variations.
/// Accuracy: ~1-2° over a few centuries near J2000.
/// </summary>
public static class PlanetaryCalculator
{
    private const double DEG = Math.PI / 180.0;
    private const double RAD = 180.0 / Math.PI;
    private const double AU_TO_KM = 149597870.7;

    // Orbital elements at J2000.0 and their rates per Julian century
    // Format: [a, e, I, L, long_peri, long_node, da, de, dI, dL, dlong_peri, dlong_node]
    // a in AU, e=eccentricity, I=inclination deg, L=mean lon deg, 
    // long_peri=longitude of perihelion deg, long_node=lon of ascending node deg
    // Rates per Julian century
    // Source: Meeus Table 31.a (J2000.0 elements)
    private static readonly double[,] PlanetElements = {
        // Mercury
        { 0.38709927, 0.20563593, 7.00497902,  252.25032350, 77.45779628,  48.33076593,
          0.00000037, 0.00001906, -0.00594749, 149472.67411175,  0.16047689, -0.12534081 },
        // Venus
        { 0.72333566, 0.00677672, 3.39467605,  181.97909950, 131.60246718,  76.67984255,
          0.00000390,-0.00004107, -0.00078890,  58517.81538729,  0.00268329, -0.27769418 },
        // Earth (EMB)
        { 1.00000261, 0.01671123,-0.00001531,  100.46457166, 102.93768193,   0.0,
          0.00000562,-0.00004392, -0.01294668,   35999.37244981,  0.32327364,  0.0 },
        // Mars
        { 1.52371034, 0.09339410, 1.84969142,   -4.55343205,  -23.94362959,  49.55953891,
          0.00001847, 0.00007882, -0.00813131,   19140.30268499,  0.44441088, -0.29257343 },
        // Jupiter
        { 5.20288700, 0.04838624, 1.30439695,   34.39644051,  14.72847983, 100.47390909,
         -0.00011607,-0.00013253, -0.00183714,    3034.74612775,  0.21252668,  0.20469106 },
        // Saturn
        { 9.53667594, 0.05386179, 2.48599187,   49.95424423,  92.59887831, 113.66242448,
         -0.00125060,-0.00050991,  0.00193609,    1222.49362201, -0.41897216, -0.28867794 },
        // Uranus
        {19.18916464, 0.04725744, 0.77263783,  313.23810451, 170.95427630,  74.01692503,
         -0.00196176,-0.00004397, -0.00242939,     428.48202785,  0.40805281,  0.04240589 },
        // Neptune
        {30.06992276, 0.00859048, 1.77004347,  -55.12002969,  44.96476227, 131.78422574,
          0.00026291, 0.00005105,  0.00035372,     218.45945325, -0.32241464, -0.00508664 },
    };

    private static readonly string[] PlanetNames = {
        "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune"
    };

    private static readonly string[] PlanetColors = {
        "#b5b5b5", "#e8cda0", "#4fa3e0", "#c1440e", "#c9a84c", "#e8d5a3", "#7de8e8", "#4169e1"
    };

    private static readonly double[] PlanetRadii = {
        2439.7, 6051.8, 6371.0, 3389.5, 69911.0, 58232.0, 25362.0, 24622.0
    };

    private static readonly double[] PlanetMasses = {
        3.301e23, 4.868e24, 5.972e24, 6.417e23, 1.899e27, 5.683e26, 8.681e25, 1.024e26
    };

    /// <summary>
    /// Calculate heliocentric rectangular coordinates for all 8 planets.
    /// T = Julian centuries from J2000.
    /// Returns array of (x,y,z) in AU.
    /// </summary>
    public static (double X, double Y, double Z)[] GetHeliocentricXYZ(double T)
    {
        var results = new (double X, double Y, double Z)[8];

        for (int p = 0; p < 8; p++)
        {
            double a = PlanetElements[p, 0] + PlanetElements[p, 6] * T;
            double e = PlanetElements[p, 1] + PlanetElements[p, 7] * T;
            double I = (PlanetElements[p, 2] + PlanetElements[p, 8] * T) * DEG;
            double L = AstroTime.Normalize360(PlanetElements[p, 3] + PlanetElements[p, 9] * T) * DEG;
            double longPeri = AstroTime.Normalize360(PlanetElements[p, 4] + PlanetElements[p, 10] * T) * DEG;
            double longNode = AstroTime.Normalize360(PlanetElements[p, 5] + PlanetElements[p, 11] * T) * DEG;

            // Mean anomaly
            double M = AstroTime.NormalizeTwoPi(L - longPeri);

            // Eccentric anomaly via Kepler's equation
            double E = AstroTime.SolveKepler(M, e);

            // True anomaly
            double nu = 2.0 * Math.Atan2(
                Math.Sqrt(1 + e) * Math.Sin(E / 2.0),
                Math.Sqrt(1 - e) * Math.Cos(E / 2.0));

            // Distance
            double r = a * (1.0 - e * Math.Cos(E));

            // Heliocentric coordinates in orbital plane
            double xOrb = r * Math.Cos(nu);
            double yOrb = r * Math.Sin(nu);

            // Argument of perihelion
            double omega = longPeri - longNode; // in radians

            // Rotate to ecliptic plane
            double cosO = Math.Cos(longNode);
            double sinO = Math.Sin(longNode);
            double cosI = Math.Cos(I);
            double sinI = Math.Sin(I);
            double cosW = Math.Cos(omega);
            double sinW = Math.Sin(omega);

            double x = r * (cosO * Math.Cos(nu + omega) - sinO * Math.Sin(nu + omega) * cosI);
            double y = r * (sinO * Math.Cos(nu + omega) + cosO * Math.Sin(nu + omega) * cosI);
            double z = r * Math.Sin(nu + omega) * sinI;

            results[p] = (x, y, z);
        }

        return results;
    }

    /// <summary>
    /// Get all planet positions as CelestialBody list.
    /// </summary>
    public static List<Planet> GetPlanets(double jd, double latitudeDeg, double longitudeDeg)
    {
        double T = AstroTime.JulianCenturies(jd);
        var helioXYZ = GetHeliocentricXYZ(T);
        double lst = AstroTime.LocalApparentSiderealTime(jd, longitudeDeg);
        double epsilon = CoordinateConverter.ApparentObliquity(T);

        // Earth's position (index 2)
        var (ex, ey, ez) = helioXYZ[2];

        // Sun's position for elongation calculations
        var sunPos = SolarCalculator.GetSunPosition(jd, latitudeDeg, longitudeDeg);

        var planets = new List<Planet>();
        for (int p = 0; p < 8; p++)
        {
            if (p == 2) continue; // Skip Earth

            var (hx, hy, hz) = helioXYZ[p];

            // Geocentric rectangular
            double gx = hx - ex;
            double gy = hy - ey;
            double gz = hz - ez;

            // Distance from Earth in AU
            double dist = Math.Sqrt(gx * gx + gy * gy + gz * gz);

            // Light-travel time correction (Meeus Ch. 33)
            double tau = 0.0057755183 * dist; // days
            double jdCorr = jd - tau;
            double Tc = AstroTime.JulianCenturies(jdCorr);
            var helioXYZCorr = GetHeliocentricXYZ(Tc);
            var (hxc, hyc, hzc) = helioXYZCorr[p];
            gx = hxc - ex; gy = hyc - ey; gz = hzc - ez;
            dist = Math.Sqrt(gx * gx + gy * gy + gz * gz);

            // Geocentric ecliptic coordinates
            double lambda = Math.Atan2(gy, gx) * RAD;
            lambda = AstroTime.Normalize360(lambda);
            double beta = Math.Asin(gz / dist) * RAD;

            // FK5 correction (Meeus Ch. 33, small corrections < 0.01°)
            double lambdaPrime = lambda - T * (1.397 + 0.00031 * T);
            double dLambda = -0.09033 + 0.03916 * (Math.Cos(lambdaPrime * DEG) - Math.Sin(lambdaPrime * DEG)) * Math.Tan(beta * DEG);
            double dBeta = 0.03916 * (Math.Cos(lambdaPrime * DEG) - Math.Sin(lambdaPrime * DEG));
            lambda += dLambda / 3600.0;
            beta += dBeta / 3600.0;

            // Apparent longitude (aberration ~20.5")
            lambda -= 0.005693 / dist;

            // Equatorial coordinates
            var (ra, dec) = CoordinateConverter.EclipticToEquatorial(lambda, beta, epsilon);

            // Horizontal coordinates
            var (alt, az) = CoordinateConverter.EquatorialToHorizontal(ra, dec, lst, latitudeDeg);

            // Visual magnitude (simplified formula)
            double mag = GetPlanetMagnitude(p, dist, Math.Sqrt(hx * hx + hy * hy + hz * hz), 0);

            // Phase angle i (Meeus Eq. 48.3)
            double rHelio = Math.Sqrt(hxc * hxc + hyc * hyc + hzc * hzc);
            double sunDist = sunPos.Distance;
            double cosPhaseAngle = (rHelio * rHelio + dist * dist - sunDist * sunDist) / (2 * rHelio * dist);
            cosPhaseAngle = Math.Max(-1.0, Math.Min(1.0, cosPhaseAngle));
            double phaseAngle = Math.Acos(cosPhaseAngle) * RAD;
            double illum = (1.0 + Math.Cos(phaseAngle * DEG)) / 2.0;

            // Elongation from sun
            double elongation = Math.Acos(
                Math.Max(-1.0, Math.Min(1.0,
                    (gx * (-ex) + gy * (-ey) + gz * (-ez))
                    / (dist * Math.Sqrt(ex * ex + ey * ey + ez * ez))
                ))) * RAD;

            var planet = new Planet
            {
                Name = PlanetNames[p],
                PlanetNumber = p + 1,
                BodyType = CelestialBodyType.Planet,
                Color = PlanetColors[p],
                RadiusKm = PlanetRadii[p],
                MassKg = PlanetMasses[p],
                Position = new SkyPosition
                {
                    RightAscension = ra,
                    Declination = dec,
                    Altitude = alt,
                    Azimuth = az,
                    Distance = dist,
                    EclipticLongitude = lambda,
                    EclipticLatitude = beta,
                    Magnitude = mag,
                    PhaseAngle = phaseAngle,
                    IlluminatedFraction = illum,
                    X = hx, Y = hy, Z = hz
                },
                ElongationFromSun = elongation
            };

            planets.Add(planet);
        }

        return planets;
    }

    private static double GetPlanetMagnitude(int planetIndex, double dist, double r, double phaseAngle)
    {
        // Approximate visual magnitude (Van Flandern & Pulkkinen method)
        return planetIndex switch
        {
            0 => -0.42 + 5 * Math.Log10(r * dist) + 0.0380 * phaseAngle - 0.000273 * phaseAngle * phaseAngle + 0.000002 * phaseAngle * phaseAngle * phaseAngle,
            1 => -4.40 + 5 * Math.Log10(r * dist) + 0.0009 * phaseAngle + 0.000239 * phaseAngle * phaseAngle - 0.00000065 * phaseAngle * phaseAngle * phaseAngle,
            3 => -1.52 + 5 * Math.Log10(r * dist) + 0.016 * phaseAngle,
            4 => -9.40 + 5 * Math.Log10(r * dist) + 0.005 * phaseAngle,
            5 => -8.88 + 5 * Math.Log10(r * dist),
            6 => -7.19 + 5 * Math.Log10(r * dist),
            7 => -6.87 + 5 * Math.Log10(r * dist),
            _ => 99.0
        };
    }

    /// <summary>
    /// Get heliocentric position of a planet for solar system view (2D XY in ecliptic plane).
    /// Returns (x, y) in AU.
    /// </summary>
    public static (double X, double Y) GetHeliocentricXY(int planetIndex, double jd)
    {
        double T = AstroTime.JulianCenturies(jd);
        var all = GetHeliocentricXYZ(T);
        var (x, y, _) = all[planetIndex];
        return (x, y);
    }
}
