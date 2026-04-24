namespace AstroTool.Core.Models;

public enum AstronomicalEventType
{
    Sunrise,
    Sunset,
    SolarNoon,
    CivilDawn,
    CivilDusk,
    NauticalDawn,
    NauticalDusk,
    AstronomicalDawn,
    AstronomicalDusk,
    Moonrise,
    Moonset,
    LunarTransit,
    NewMoon,
    FirstQuarter,
    FullMoon,
    LastQuarter,
    PlanetRise,
    PlanetSet,
    PlanetTransit,
    PlanetOpposition,
    PlanetConjunction,
    StarRise,
    StarSet,
    StarTransit,
    MeteorShower,
    Eclipse,
    Perihelion,
    Aphelion,
    Equinox,
    Solstice
}

public class AstronomicalEvent
{
    public AstronomicalEventType EventType { get; set; }
    public DateTime Time { get; set; }
    public string BodyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Magnitude { get; set; } // For events where magnitude matters
    public bool NotificationsEnabled { get; set; } = true;
    public int NotifyMinutesBefore { get; set; } = 15;

    public string Icon => EventType switch
    {
        AstronomicalEventType.Sunrise => "☀️",
        AstronomicalEventType.Sunset => "🌅",
        AstronomicalEventType.SolarNoon => "🌞",
        AstronomicalEventType.Moonrise => "🌙",
        AstronomicalEventType.Moonset => "🌙",
        AstronomicalEventType.LunarTransit => "🌕",
        AstronomicalEventType.FullMoon => "🌕",
        AstronomicalEventType.NewMoon => "🌑",
        AstronomicalEventType.FirstQuarter => "🌓",
        AstronomicalEventType.LastQuarter => "🌗",
        AstronomicalEventType.PlanetRise => "🪐",
        AstronomicalEventType.PlanetSet => "🪐",
        AstronomicalEventType.PlanetTransit => "🪐",
        _ => "⭐"
    };
}
