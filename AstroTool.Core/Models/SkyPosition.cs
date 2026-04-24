namespace AstroTool.Core.Models;

public class SkyPosition
{
    /// <summary>Altitude above horizon in degrees (-90 to +90)</summary>
    public double Altitude { get; set; }

    /// <summary>Azimuth from North, clockwise, in degrees (0-360)</summary>
    public double Azimuth { get; set; }

    /// <summary>Right Ascension in hours (0-24)</summary>
    public double RightAscension { get; set; }

    /// <summary>Declination in degrees (-90 to +90)</summary>
    public double Declination { get; set; }

    /// <summary>Distance from Earth in AU (or km for Moon)</summary>
    public double Distance { get; set; }

    /// <summary>Apparent visual magnitude</summary>
    public double Magnitude { get; set; }

    /// <summary>Ecliptic longitude in degrees</summary>
    public double EclipticLongitude { get; set; }

    /// <summary>Ecliptic latitude in degrees</summary>
    public double EclipticLatitude { get; set; }

    /// <summary>Heliocentric X in AU</summary>
    public double X { get; set; }

    /// <summary>Heliocentric Y in AU</summary>
    public double Y { get; set; }

    /// <summary>Heliocentric Z in AU</summary>
    public double Z { get; set; }

    /// <summary>Whether the body is above the horizon</summary>
    public bool IsAboveHorizon => Altitude > 0;

    /// <summary>Angular diameter in arc-minutes</summary>
    public double AngularDiameter { get; set; }

    /// <summary>Phase angle in degrees (0 = full, 180 = new)</summary>
    public double PhaseAngle { get; set; }

    /// <summary>Illuminated fraction (0-1)</summary>
    public double IlluminatedFraction { get; set; }
}
