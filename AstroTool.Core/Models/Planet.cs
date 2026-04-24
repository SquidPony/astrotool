namespace AstroTool.Core.Models;

public class Planet : CelestialBody
{
    public int PlanetNumber { get; set; } // 1=Mercury ... 8=Neptune
    public OrbitalElements OrbitalElements { get; set; } = new();
    public bool HasRings { get; set; }
    public List<Moon> Moons { get; set; } = new();
}

public class Moon : CelestialBody
{
    public OrbitalElements OrbitalElements { get; set; } = new();

    // Moon-specific
    public double Phase { get; set; } // 0-1 (0=new, 0.25=first quarter, 0.5=full, 0.75=last quarter)
    public string PhaseName { get; set; } = string.Empty;
    public double IlluminationPercent => Position.IlluminatedFraction * 100.0;
    public double Age { get; set; } // Days since new moon
    public double ParallaxDeg { get; set; } // Equatorial horizontal parallax
}
