namespace AstroTool.Core.Models;

public class Star : CelestialBody
{
    /// <summary>Bayer designation (e.g., "α CMa")</summary>
    public string BayerDesignation { get; set; } = string.Empty;

    /// <summary>Henry Draper catalog number</summary>
    public int HdNumber { get; set; }

    /// <summary>Right Ascension at J2000.0 in decimal hours</summary>
    public double RA2000 { get; set; }

    /// <summary>Declination at J2000.0 in decimal degrees</summary>
    public double Dec2000 { get; set; }

    /// <summary>Apparent visual magnitude</summary>
    public double ApparentMagnitude { get; set; }

    /// <summary>Absolute magnitude</summary>
    public double AbsoluteMagnitude { get; set; }

    /// <summary>Proper motion in RA (arcsec/year)</summary>
    public double ProperMotionRA { get; set; }

    /// <summary>Proper motion in Dec (arcsec/year)</summary>
    public double ProperMotionDec { get; set; }

    /// <summary>Radial velocity in km/s</summary>
    public double RadialVelocity { get; set; }

    /// <summary>Parallax in arcseconds</summary>
    public double Parallax { get; set; }
}
