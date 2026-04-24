namespace AstroTool.Core.Astronomy;

/// <summary>
/// Computes orbit path points for the solar system view.
/// Returns heliocentric XY points (in AU) for a full orbit.
/// </summary>
public static class OrbitCalculator
{
    private const double DEG = Math.PI / 180.0;
    private const double RAD = 180.0 / Math.PI;

    // Mean orbital radii in AU (semi-major axis)
    private static readonly double[] PlanetSemiMajorAxis = {
        0.387, 0.723, 1.000, 1.524, 5.203, 9.537, 19.191, 30.069
    };

    private static readonly string[] PlanetNames = {
        "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune"
    };

    /// <summary>
    /// Get orbital ellipse points for all planets and Moon.
    /// Points are heliocentric ecliptic XY in AU (Moon relative to Earth).
    /// </summary>
    public static Dictionary<string, List<(double X, double Y)>> GetAllOrbitPaths(
        double jd, int pointCount = 180)
    {
        double T = AstroTime.JulianCenturies(jd);
        var result = new Dictionary<string, List<(double X, double Y)>>();

        // Planet orbits
        for (int p = 0; p < 8; p++)
        {
            result[PlanetNames[p]] = GetPlanetOrbitPath(p, T, pointCount);
        }

        // Moon orbit (geocentric, scaled to show near Earth)
        result["Moon"] = GetMoonOrbitPath(T, pointCount);

        return result;
    }

    /// <summary>
    /// Get orbit ellipse path for one planet.
    /// </summary>
    public static List<(double X, double Y)> GetPlanetOrbitPath(int planetIndex, double T, int pointCount = 180)
    {
        var points = new List<(double X, double Y)>(pointCount + 1);

        // Sample the planet position at evenly-spaced true anomalies
        // Uses full orbital elements from PlanetaryCalculator
        // For display purposes, we approximate as a circle at mean distance
        double a = PlanetSemiMajorAxis[planetIndex];
        double e = GetEccentricity(planetIndex, T);
        double b = a * Math.Sqrt(1.0 - e * e);
        double omega = GetArgPerihelion(planetIndex, T); // degrees

        // Get the actual inclination and node
        double node = GetLongNode(planetIndex, T);

        for (int i = 0; i <= pointCount; i++)
        {
            double nu = 2.0 * Math.PI * i / pointCount; // true anomaly

            // Position in orbital plane
            double r = a * (1.0 - e * e) / (1.0 + e * Math.Cos(nu));
            double xOrb = r * Math.Cos(nu);
            double yOrb = r * Math.Sin(nu);

            // Rotate by argument of perihelion and ascending node
            // (simplified: project to ecliptic plane, ignoring inclination for 2D view)
            double omegaRad = omega * DEG;
            double nodeRad = node * DEG;

            double x = xOrb * (Math.Cos(nodeRad) * Math.Cos(omegaRad) - Math.Sin(nodeRad) * Math.Sin(omegaRad))
                     - yOrb * (Math.Cos(nodeRad) * Math.Sin(omegaRad) + Math.Sin(nodeRad) * Math.Cos(omegaRad));
            double y = xOrb * (Math.Sin(nodeRad) * Math.Cos(omegaRad) + Math.Cos(nodeRad) * Math.Sin(omegaRad))
                     - yOrb * (Math.Sin(nodeRad) * Math.Sin(omegaRad) - Math.Cos(nodeRad) * Math.Cos(omegaRad));

            points.Add((x, y));
        }

        return points;
    }

    /// <summary>
    /// Moon's orbit path relative to Earth, scaled in AU.
    /// Moon mean distance ≈ 0.00257 AU.
    /// </summary>
    public static List<(double X, double Y)> GetMoonOrbitPath(double T, int pointCount = 60)
    {
        var points = new List<(double X, double Y)>(pointCount + 1);
        double a = 0.00257;  // AU
        double e = 0.0549;
        double b = a * Math.Sqrt(1 - e * e);

        for (int i = 0; i <= pointCount; i++)
        {
            double angle = 2.0 * Math.PI * i / pointCount;
            double x = a * Math.Cos(angle);
            double y = b * Math.Sin(angle);
            points.Add((x, y));
        }

        return points;
    }

    /// <summary>
    /// Get current positions of all planets and Moon for the solar system view.
    /// Returns heliocentric ecliptic XY in AU (Moon position is geocentric).
    /// </summary>
    public static Dictionary<string, (double X, double Y)> GetCurrentPositions(double jd)
    {
        double T = AstroTime.JulianCenturies(jd);
        var all = PlanetaryCalculator.GetHeliocentricXYZ(T);
        var result = new Dictionary<string, (double X, double Y)>();

        string[] names = { "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune" };
        for (int i = 0; i < 8; i++)
        {
            result[names[i]] = (all[i].X, all[i].Y);
        }

        // Moon: compute geocentric ecliptic position
        var moonPos = MoonCalculator.GetMoonPosition(jd, 0, 0);
        // Convert from km to AU and offset by Earth's position
        double moonX = all[2].X + moonPos.X;
        double moonY = all[2].Y + moonPos.Y;
        result["Moon"] = (moonX, moonY);

        return result;
    }

    // Orbital element accessors (simplified from Table 31.a)
    private static readonly double[] Eccentricities = {
        0.20563593, 0.00677672, 0.01671123, 0.09339410,
        0.04838624, 0.05386179, 0.04725744, 0.00859048
    };

    private static readonly double[] EccentricityRates = {
        0.00001906, -0.00004107, -0.00004392, 0.00007882,
        -0.00013253, -0.00050991, -0.00004397, 0.00005105
    };

    private static readonly double[] ArgPerihelions = {
        77.45779628, 131.60246718, 102.93768193, -23.94362959,
        14.72847983, 92.59887831, 170.95427630, 44.96476227
    };

    private static readonly double[] LongNodes = {
        48.33076593, 76.67984255, 0.0, 49.55953891,
        100.47390909, 113.66242448, 74.01692503, 131.78422574
    };

    private static double GetEccentricity(int p, double T) =>
        Eccentricities[p] + EccentricityRates[p] * T;

    private static double GetArgPerihelion(int p, double T) =>
        AstroTime.Normalize360(ArgPerihelions[p]);

    private static double GetLongNode(int p, double T) =>
        AstroTime.Normalize360(LongNodes[p]);
}
