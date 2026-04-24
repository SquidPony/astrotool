using AstroTool.Core.Models;

namespace AstroTool.Core.Astronomy;

/// <summary>
/// Computes rise, transit, and set times for celestial bodies.
/// Meeus Chapter 15.
/// </summary>
public static class EventCalculator
{
    private const double DEG = Math.PI / 180.0;
    private const double RAD = 180.0 / Math.PI;

    // Standard altitude corrections
    public const double H0_STARS    = -0.5667;  // stars and planets
    public const double H0_SUN      = -0.8333;  // Sun (refraction + semi-diameter)
    public const double H0_MOON     = 0.7275;   // Moon: depends on parallax (approximate)
    public const double H0_CIVIL    = -6.0;     // civil twilight
    public const double H0_NAUTICAL = -12.0;    // nautical twilight
    public const double H0_ASTRO    = -18.0;    // astronomical twilight

    /// <summary>
    /// Compute rise, transit, and set times for a body.
    /// Meeus Chapter 15 algorithm.
    /// Returns (rise, transit, set) as JD values; double.NaN if circumpolar or never rises.
    /// </summary>
    public static (double Rise, double Transit, double Set) GetRiseTransitSet(
        double jd0,
        double ra,   // degrees
        double dec,  // degrees
        double latitudeDeg,
        double longitudeDeg,
        double h0 = H0_STARS)
    {
        // Cosine of the hour angle at rise/set (Meeus 15.1)
        double cosH0 = (Math.Sin(h0 * DEG) - Math.Sin(latitudeDeg * DEG) * Math.Sin(dec * DEG))
                     / (Math.Cos(latitudeDeg * DEG) * Math.Cos(dec * DEG));

        if (cosH0 < -1.0) return (double.NaN, double.NaN, double.NaN); // circumpolar
        if (cosH0 > 1.0)  return (double.NaN, double.NaN, double.NaN); // never rises

        double H0 = Math.Acos(cosH0) * RAD;

        // Sidereal time at 0h UT for day of jd0
        double jd0noon = Math.Floor(jd0 - 0.5) + 0.5;
        double theta0 = AstroTime.GreenwichMeanSiderealTime(jd0noon + 1.0);

        // Approximate transit/rise/set times (fraction of day)
        double m0 = (ra - longitudeDeg - theta0) / 360.0;
        double m1 = m0 - H0 / 360.0;
        double m2 = m0 + H0 / 360.0;

        m0 = Frac(m0);
        m1 = Frac(m1);
        m2 = Frac(m2);

        // Iterate to improve accuracy (Meeus p.102)
        for (int iter = 0; iter < 2; iter++)
        {
            double lst0 = theta0 + 360.98564724 * m0;
            double H_transit = AstroTime.Normalize180(lst0 - (-longitudeDeg) - ra);
            double dm0 = -H_transit / 360.0;
            m0 = Frac(m0 + dm0);

            for (int j = 1; j <= 2; j++)
            {
                double m = (j == 1) ? m1 : m2;
                double lst = theta0 + 360.98564724 * m;
                double H = AstroTime.Normalize180(lst - (-longitudeDeg) - ra);
                double h = Math.Asin(
                    Math.Sin(latitudeDeg * DEG) * Math.Sin(dec * DEG)
                    + Math.Cos(latitudeDeg * DEG) * Math.Cos(dec * DEG) * Math.Cos(H * DEG)
                ) * RAD;
                double dm = (h - h0) / (360.0 * Math.Cos(dec * DEG) * Math.Cos(latitudeDeg * DEG) * Math.Sin(H * DEG));
                if (j == 1) m1 = Frac(m1 + dm);
                else        m2 = Frac(m2 + dm);
            }
        }

        double riseJD    = jd0noon + m1;
        double transitJD = jd0noon + m0;
        double setJD     = jd0noon + m2;

        return (riseJD, transitJD, setJD);
    }

    /// <summary>
    /// Compute upcoming astronomical events for a given date and location.
    /// </summary>
    public static List<AstronomicalEvent> GetUpcomingEvents(
        double jd, double latitudeDeg, double longitudeDeg, int dayCount = 7)
    {
        var events = new List<AstronomicalEvent>();

        for (int d = 0; d < dayCount; d++)
        {
            double dayJD = Math.Floor(jd) + d;
            var sunPos  = SolarCalculator.GetSunPosition(dayJD, latitudeDeg, longitudeDeg);
            var moonPos = MoonCalculator.GetMoonPosition(dayJD, latitudeDeg, longitudeDeg);

            // Sun rise/transit/set
            var (sunRise, sunTransit, sunSet) = GetRiseTransitSet(
                dayJD, sunPos.RightAscension, sunPos.Declination,
                latitudeDeg, longitudeDeg, H0_SUN);

            if (!double.IsNaN(sunRise))
                events.Add(MakeEvent(AstronomicalEventType.Sunrise, sunRise, "Sun", "Sun rises"));

            if (!double.IsNaN(sunSet))
                events.Add(MakeEvent(AstronomicalEventType.Sunset, sunSet, "Sun", "Sun sets"));

            // Moon rise/transit/set
            var (moonRise, moonTransit, moonSet) = GetRiseTransitSet(
                dayJD, moonPos.RightAscension, moonPos.Declination,
                latitudeDeg, longitudeDeg, H0_MOON);

            if (!double.IsNaN(moonRise))
                events.Add(MakeEvent(AstronomicalEventType.Moonrise, moonRise, "Moon", "Moon rises"));

            if (!double.IsNaN(moonSet))
                events.Add(MakeEvent(AstronomicalEventType.Moonset, moonSet, "Moon", "Moon sets"));

            // Moon phases
            events.AddRange(GetMoonPhaseEvents(dayJD));

            // Planetary events (only compute once)
            if (d == 0)
                events.AddRange(GetPlanetaryEvents(dayJD, latitudeDeg, longitudeDeg));

            // Twilight
            var (cr, _, cs) = GetRiseTransitSet(dayJD, sunPos.RightAscension, sunPos.Declination,
                latitudeDeg, longitudeDeg, H0_CIVIL);
            if (!double.IsNaN(cr))
                events.Add(MakeEvent(AstronomicalEventType.CivilDawn, cr, "Sun", "Civil twilight begins"));
            if (!double.IsNaN(cs))
                events.Add(MakeEvent(AstronomicalEventType.CivilDusk, cs, "Sun", "Civil twilight ends"));

            var (nr, _, ns) = GetRiseTransitSet(dayJD, sunPos.RightAscension, sunPos.Declination,
                latitudeDeg, longitudeDeg, H0_NAUTICAL);
            if (!double.IsNaN(nr))
                events.Add(MakeEvent(AstronomicalEventType.NauticalDawn, nr, "Sun", "Nautical twilight begins"));
            if (!double.IsNaN(ns))
                events.Add(MakeEvent(AstronomicalEventType.NauticalDusk, ns, "Sun", "Nautical twilight ends"));

            var (ar, _, aSet) = GetRiseTransitSet(dayJD, sunPos.RightAscension, sunPos.Declination,
                latitudeDeg, longitudeDeg, H0_ASTRO);
            if (!double.IsNaN(ar))
                events.Add(MakeEvent(AstronomicalEventType.AstronomicalDawn, ar, "Sun", "Astronomical twilight begins"));
            if (!double.IsNaN(aSet))
                events.Add(MakeEvent(AstronomicalEventType.AstronomicalDusk, aSet, "Sun", "Astronomical twilight ends"));
        }

        events.Sort((a, b) => a.Time.CompareTo(b.Time));
        return events;
    }

    private static AstronomicalEvent MakeEvent(
        AstronomicalEventType type, double jd, string bodyName, string description) =>
        new()
        {
            EventType = type,
            Time = AstroTime.FromJulianDate(jd),
            BodyName = bodyName,
            Description = description
        };

    private static IEnumerable<AstronomicalEvent> GetMoonPhaseEvents(double jd)
    {
        double phase = MoonCalculator.GetMoonPhase(jd);
        double nextPhase = MoonCalculator.GetMoonPhase(jd + 1);

        var events = new List<AstronomicalEvent>();

        void CheckCrossing(double p1, double p2, double target,
            AstronomicalEventType evType, string desc)
        {
            double dp1 = Math.Abs(p1 - target);
            double dp2 = Math.Abs(p2 - target);
            if (dp1 < 0.05 && dp2 >= dp1)
                events.Add(MakeEvent(evType, jd, "Moon", desc));
        }

        CheckCrossing(phase, nextPhase, 0.0,  AstronomicalEventType.NewMoon,     "New Moon");
        CheckCrossing(phase, nextPhase, 0.25, AstronomicalEventType.FirstQuarter,"First Quarter");
        CheckCrossing(phase, nextPhase, 0.5,  AstronomicalEventType.FullMoon,    "Full Moon");
        CheckCrossing(phase, nextPhase, 0.75, AstronomicalEventType.LastQuarter, "Last Quarter");

        return events;
    }

    private static IEnumerable<AstronomicalEvent> GetPlanetaryEvents(
        double jd, double latitudeDeg, double longitudeDeg)
    {
        var events = new List<AstronomicalEvent>();
        var planets = PlanetaryCalculator.GetPlanets(jd, latitudeDeg, longitudeDeg);

        foreach (var p in planets)
        {
            if (p.ElongationFromSun < 10)
                events.Add(MakeEvent(AstronomicalEventType.PlanetConjunction, jd,
                    p.Name, $"{p.Name} near conjunction with Sun"));
            else if (p.ElongationFromSun > 170)
                events.Add(MakeEvent(AstronomicalEventType.PlanetOpposition, jd,
                    p.Name, $"{p.Name} at opposition - best visibility"));
        }

        return events;
    }

    private static double Frac(double x) => x - Math.Floor(x);
}
