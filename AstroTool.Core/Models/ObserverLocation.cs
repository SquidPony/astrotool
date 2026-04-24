namespace AstroTool.Core.Models;

public class ObserverLocation
{
    public double Latitude { get; set; }    // degrees, positive = North
    public double Longitude { get; set; }   // degrees, positive = East
    public double AltitudeMeters { get; set; }
    public string? Name { get; set; }
    public string? TimeZoneId { get; set; }

    public static ObserverLocation Default => new()
    {
        Latitude = 40.7128,   // New York City
        Longitude = -74.0060,
        AltitudeMeters = 10,
        Name = "New York, NY"
    };

    public override string ToString() =>
        $"{Name ?? "Unknown"} ({Latitude:F4}°, {Longitude:F4}°)";
}
