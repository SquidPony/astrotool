using AstroTool.Core.Astronomy;
using AstroTool.Core.Models;

namespace AstroTool.Core.Services;

/// <summary>
/// Main astronomy service that orchestrates all calculations.
/// Thread-safe; uses caching to avoid repeated expensive computations.
/// </summary>
public class AstronomyService
{
    private ObserverLocation _location = ObserverLocation.Default;
    private DateTime _simulatedTime = DateTime.UtcNow;
    private bool _useRealTime = true;

    // Cache fields
    private double _cachedJd = 0;
    private double _cachedLat = double.NaN;
    private double _cachedLon = double.NaN;
    private List<Planet>? _cachedPlanets;
    private List<Star>? _cachedStars;
    private SkyPosition? _cachedSunPos;
    private SkyPosition? _cachedMoonPos;
    private List<AstronomicalEvent>? _cachedEvents;

    public event Action? OnUpdate;

    public ObserverLocation Location
    {
        get => _location;
        set
        {
            _location = value;
            InvalidateCache();
        }
    }

    public DateTime CurrentTime => _useRealTime ? DateTime.UtcNow : _simulatedTime;

    public double CurrentJD => AstroTime.ToJulianDate(CurrentTime);

    public bool UseRealTime
    {
        get => _useRealTime;
        set
        {
            _useRealTime = value;
            if (value) _simulatedTime = DateTime.UtcNow;
        }
    }

    public void SetSimulatedTime(DateTime utcTime)
    {
        _useRealTime = false;
        _simulatedTime = utcTime;
        InvalidateCache();
    }

    public void AdvanceTime(TimeSpan delta)
    {
        if (_useRealTime) _simulatedTime = DateTime.UtcNow;
        _simulatedTime += delta;
        _useRealTime = false;
        InvalidateCache();
    }

    private void InvalidateCache()
    {
        _cachedPlanets = null;
        _cachedStars = null;
        _cachedSunPos = null;
        _cachedMoonPos = null;
        _cachedEvents = null;
        _cachedJd = 0;
        _cachedLat = double.NaN;
        _cachedLon = double.NaN;
    }

    private bool IsCacheValid()
    {
        double jd = CurrentJD;
        return Math.Abs(jd - _cachedJd) < 1.0 / 86400.0 // within 1 second
            && Math.Abs(_cachedLat - _location.Latitude) < 0.0001
            && Math.Abs(_cachedLon - _location.Longitude) < 0.0001;
    }

    private void EnsureCache()
    {
        if (IsCacheValid()) return;

        double jd = CurrentJD;
        double lat = _location.Latitude;
        double lon = _location.Longitude;

        _cachedSunPos = SolarCalculator.GetSunPosition(jd, lat, lon);
        _cachedMoonPos = MoonCalculator.GetMoonPosition(jd, lat, lon);
        _cachedPlanets = PlanetaryCalculator.GetPlanets(jd, lat, lon);
        _cachedStars = StarCatalog.GetStars(jd, lat, lon);

        _cachedJd = jd;
        _cachedLat = lat;
        _cachedLon = lon;
    }

    public SkyPosition GetSunPosition()
    {
        EnsureCache();
        return _cachedSunPos!;
    }

    public SkyPosition GetMoonPosition()
    {
        EnsureCache();
        return _cachedMoonPos!;
    }

    public List<Planet> GetPlanets()
    {
        EnsureCache();
        return _cachedPlanets!;
    }

    public List<Star> GetStars(double limitingMagnitude = 6.5)
    {
        EnsureCache();
        return _cachedStars!.Where(s => s.Position.Magnitude <= limitingMagnitude).ToList();
    }

    public List<AstronomicalEvent> GetUpcomingEvents(int dayCount = 7)
    {
        if (_cachedEvents == null)
        {
            _cachedEvents = EventCalculator.GetUpcomingEvents(
                CurrentJD, _location.Latitude, _location.Longitude, dayCount);
        }
        return _cachedEvents;
    }

    public string GetMoonPhaseName()
    {
        double phase = MoonCalculator.GetMoonPhase(CurrentJD);
        return MoonCalculator.GetPhaseName(phase);
    }

    public double GetMoonPhase() => MoonCalculator.GetMoonPhase(CurrentJD);

    public Dictionary<string, List<(double X, double Y)>> GetOrbitPaths(int points = 180)
        => OrbitCalculator.GetAllOrbitPaths(CurrentJD, points);

    public Dictionary<string, (double X, double Y)> GetCurrentPositions()
        => OrbitCalculator.GetCurrentPositions(CurrentJD);

    /// <summary>
    /// Get rise/transit/set times for any body given its current position.
    /// </summary>
    public (DateTime? Rise, DateTime? Transit, DateTime? Set) GetRiseTransitSet(
        double ra, double dec, double h0 = EventCalculator.H0_STARS)
    {
        var (r, t, s) = EventCalculator.GetRiseTransitSet(
            CurrentJD, ra, dec, _location.Latitude, _location.Longitude, h0);

        return (
            double.IsNaN(r) ? null : AstroTime.FromJulianDate(r),
            double.IsNaN(t) ? null : AstroTime.FromJulianDate(t),
            double.IsNaN(s) ? null : AstroTime.FromJulianDate(s)
        );
    }

    /// <summary>
    /// Convert horizontal (alt/az) coords to equatorial.
    /// </summary>
    public (double RA, double Dec) HorizontalToEquatorial(double altDeg, double azDeg)
    {
        double lst = AstroTime.LocalApparentSiderealTime(CurrentJD, _location.Longitude);
        return CoordinateConverter.HorizontalToEquatorial(altDeg, azDeg, lst, _location.Latitude);
    }

    /// <summary>
    /// Local Apparent Sidereal Time in degrees.
    /// </summary>
    public double GetLAST() =>
        AstroTime.LocalApparentSiderealTime(CurrentJD, _location.Longitude);

    /// <summary>
    /// Check if dark sky conditions (astronomical night).
    /// </summary>
    public bool IsDarkSky()
    {
        var sun = GetSunPosition();
        return sun.Altitude < EventCalculator.H0_ASTRO;
    }

    /// <summary>
    /// Get bodies sorted by altitude (highest first) for a given filter.
    /// </summary>
    public List<CelestialBody> GetVisibleBodies(
        bool includeSun = true, bool includeMoon = true,
        bool includePlanets = true, bool includeStars = true,
        double minAlt = 0.0)
    {
        var bodies = new List<CelestialBody>();

        if (includeSun)
        {
            var pos = GetSunPosition();
            if (pos.Altitude >= minAlt)
                bodies.Add(new CelestialBody
                {
                    Name = "Sun",
                    BodyType = CelestialBodyType.Sun,
                    Color = "#fff700",
                    Position = pos
                });
        }

        if (includeMoon)
        {
            var pos = GetMoonPosition();
            if (pos.Altitude >= minAlt)
                bodies.Add(new Moon
                {
                    Name = "Moon",
                    BodyType = CelestialBodyType.Moon,
                    Color = "#c8c8c8",
                    Phase = GetMoonPhase(),
                    PhaseName = GetMoonPhaseName(),
                    Position = pos
                });
        }

        if (includePlanets)
        {
            foreach (var p in GetPlanets())
                if (p.Position.Altitude >= minAlt)
                    bodies.Add(p);
        }

        if (includeStars)
        {
            foreach (var s in GetStars())
                if (s.Position.Altitude >= minAlt)
                    bodies.Add(s);
        }

        bodies.Sort((a, b) => b.Position.Altitude.CompareTo(a.Position.Altitude));
        return bodies;
    }
}
