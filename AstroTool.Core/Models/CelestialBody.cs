namespace AstroTool.Core.Models;

public enum CelestialBodyType
{
    Star,
    Sun,
    Planet,
    DwarfPlanet,
    Moon,
    Comet,
    Asteroid,
    DeepSkyObject
}

public class CelestialBody
{
    public string Name { get; set; } = string.Empty;
    public string? AlternateName { get; set; }
    public CelestialBodyType BodyType { get; set; }
    public SkyPosition Position { get; set; } = new();

    /// <summary>Color for rendering (CSS color string)</summary>
    public string Color { get; set; } = "#ffffff";

    /// <summary>Radius of body in km</summary>
    public double RadiusKm { get; set; }

    /// <summary>Mass in kg</summary>
    public double MassKg { get; set; }

    /// <summary>Parent body name (e.g., "Jupiter" for Io)</summary>
    public string? ParentBody { get; set; }

    /// <summary>Rise time for the observing date (UTC)</summary>
    public DateTime? RiseTime { get; set; }

    /// <summary>Transit (culmination) time (UTC)</summary>
    public DateTime? TransitTime { get; set; }

    /// <summary>Set time for the observing date (UTC)</summary>
    public DateTime? SetTime { get; set; }

    public bool IsVisible { get; set; } = true;
    public bool ShowOrbit { get; set; } = true;

    // Additional physical properties
    public double RotationPeriodHours { get; set; }
    public double OrbitalPeriodDays { get; set; }
    public string? SpectralType { get; set; }

    // For stars: distance in light-years
    public double DistanceLy { get; set; }

    // Current constellation
    public string? Constellation { get; set; }

    // Elongation from Sun in degrees
    public double ElongationFromSun { get; set; }
}
