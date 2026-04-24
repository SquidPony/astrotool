namespace AstroTool.Core.Models;

/// <summary>Keplerian orbital elements at a given epoch</summary>
public class OrbitalElements
{
    /// <summary>Semi-major axis in AU</summary>
    public double SemiMajorAxis { get; set; }

    /// <summary>Eccentricity (0 = circle, 1 = parabola)</summary>
    public double Eccentricity { get; set; }

    /// <summary>Inclination to the ecliptic in degrees</summary>
    public double Inclination { get; set; }

    /// <summary>Longitude of ascending node in degrees</summary>
    public double LongitudeOfAscendingNode { get; set; }

    /// <summary>Argument of perihelion in degrees</summary>
    public double ArgumentOfPerihelion { get; set; }

    /// <summary>Mean longitude at epoch in degrees</summary>
    public double MeanLongitude { get; set; }

    /// <summary>Mean anomaly at epoch in degrees</summary>
    public double MeanAnomaly { get; set; }

    /// <summary>Longitude of perihelion = LAN + ArgPeri</summary>
    public double LongitudeOfPerihelion => LongitudeOfAscendingNode + ArgumentOfPerihelion;

    /// <summary>Epoch as Julian Date (J2000 = 2451545.0)</summary>
    public double Epoch { get; set; } = 2451545.0;

    // Rate of change per Julian century
    public double dSemiMajorAxis { get; set; }
    public double dEccentricity { get; set; }
    public double dInclination { get; set; }
    public double dLongitudeOfAscendingNode { get; set; }
    public double dArgumentOfPerihelion { get; set; }
    public double dMeanLongitude { get; set; }
}
