using AstroTool.Core.Models;
using AstroTool.Core.Services;
using Xunit;

namespace AstroTool.Core.Tests;

public class AstronomyServiceTests
{
    private static AstronomyService CreateService(double lat = 51.5, double lon = 0.0)
    {
        var svc = new AstronomyService();
        svc.Location = new ObserverLocation { Latitude = lat, Longitude = lon };
        svc.SetSimulatedTime(new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        return svc;
    }

    [Fact]
    public void GetSunPosition_ReturnsValidPosition()
    {
        var svc = CreateService();
        SkyPosition pos = svc.GetSunPosition();
        Assert.NotNull(pos);
        Assert.InRange(pos.Altitude, -90.0, 90.0);
        Assert.InRange(pos.RightAscension, 0.0, 24.0);
    }

    [Fact]
    public void GetMoonPosition_ReturnsValidPosition()
    {
        var svc = CreateService();
        SkyPosition pos = svc.GetMoonPosition();
        Assert.NotNull(pos);
        Assert.InRange(pos.Altitude, -90.0, 90.0);
        Assert.InRange(pos.Distance, 350_000.0, 410_000.0);
    }

    [Fact]
    public void GetPlanets_ReturnsNonEmptyList()
    {
        var svc = CreateService();
        List<Planet> planets = svc.GetPlanets();
        Assert.NotNull(planets);
        Assert.NotEmpty(planets);
    }

    [Fact]
    public void GetPlanets_AllPlanetsHaveValidPositions()
    {
        var svc = CreateService();
        List<Planet> planets = svc.GetPlanets();
        foreach (var planet in planets)
        {
            Assert.InRange(planet.Position.Altitude, -90.0, 90.0);
            Assert.InRange(planet.Position.RightAscension, 0.0, 24.0);
        }
    }

    [Fact]
    public void GetMoonPhase_ReturnsValueInRange()
    {
        var svc = CreateService();
        double phase = svc.GetMoonPhase();
        Assert.InRange(phase, 0.0, 1.0);
    }

    [Fact]
    public void GetMoonPhaseName_ReturnsNonEmptyString()
    {
        var svc = CreateService();
        string name = svc.GetMoonPhaseName();
        Assert.False(string.IsNullOrEmpty(name));
    }

    [Fact]
    public void SetSimulatedTime_ChangesCurrentTime()
    {
        var svc = new AstronomyService();
        var targetTime = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        svc.SetSimulatedTime(targetTime);
        Assert.Equal(targetTime, svc.CurrentTime);
        Assert.False(svc.UseRealTime);
    }

    [Fact]
    public void AdvanceTime_MovesTimeForward()
    {
        var svc = new AstronomyService();
        var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        svc.SetSimulatedTime(baseTime);
        svc.AdvanceTime(TimeSpan.FromHours(1));
        Assert.Equal(baseTime + TimeSpan.FromHours(1), svc.CurrentTime);
    }

    [Fact]
    public void CurrentJD_MatchesSimulatedTime()
    {
        var svc = new AstronomyService();
        var targetTime = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        svc.SetSimulatedTime(targetTime);
        Assert.Equal(2451545.0, svc.CurrentJD, 4);
    }

    [Fact]
    public void GetStars_ReturnsNonEmptyList()
    {
        var svc = CreateService();
        List<Star> stars = svc.GetStars();
        Assert.NotNull(stars);
        Assert.NotEmpty(stars);
    }

    [Fact]
    public void GetStars_BrighterMagnitudeLimitReturnsFewerStars()
    {
        var svc = CreateService();
        List<Star> allStars = svc.GetStars(limitingMagnitude: 6.5);
        List<Star> brightStars = svc.GetStars(limitingMagnitude: 2.0);
        Assert.True(brightStars.Count <= allStars.Count);
    }

    [Fact]
    public void GetUpcomingEvents_ReturnsListForSevenDays()
    {
        var svc = CreateService();
        List<AstronomicalEvent> events = svc.GetUpcomingEvents(7);
        Assert.NotNull(events);
    }

    [Fact]
    public void GetOrbitPaths_ReturnsExpectedBodies()
    {
        var svc = CreateService();
        var paths = svc.GetOrbitPaths(36);
        Assert.NotNull(paths);
        Assert.NotEmpty(paths);
    }

    [Fact]
    public void GetCurrentPositions_ReturnsExpectedBodies()
    {
        var svc = CreateService();
        var positions = svc.GetCurrentPositions();
        Assert.NotNull(positions);
        Assert.NotEmpty(positions);
    }

    [Fact]
    public void Location_Change_InvalidatesCache()
    {
        var svc = CreateService(0.0, 0.0);
        SkyPosition pos1 = svc.GetSunPosition();

        svc.Location = new ObserverLocation { Latitude = 60.0, Longitude = 25.0 };
        SkyPosition pos2 = svc.GetSunPosition();

        // Different observer location produces different altitude
        Assert.NotEqual(pos1.Altitude, pos2.Altitude);
    }

    [Fact]
    public void UseRealTime_WhenTrue_CurrentTimeChanges()
    {
        var svc = new AstronomyService();
        svc.UseRealTime = true;
        DateTime t1 = svc.CurrentTime;
        Assert.True(svc.UseRealTime);
        // Current time should be close to UtcNow
        Assert.InRange((DateTime.UtcNow - t1).TotalSeconds, -5.0, 5.0);
    }

    [Fact]
    public void GetVisibleBodies_ReturnsListWithoutExceptions()
    {
        var svc = CreateService();
        List<CelestialBody> bodies = svc.GetVisibleBodies();
        Assert.NotNull(bodies);
    }

    [Fact]
    public void GetVisibleBodies_SortedByDecreasingAltitude()
    {
        var svc = CreateService();
        List<CelestialBody> bodies = svc.GetVisibleBodies(minAlt: double.MinValue);
        for (int i = 1; i < bodies.Count; i++)
            Assert.True(bodies[i - 1].Position.Altitude >= bodies[i].Position.Altitude);
    }

    [Fact]
    public void GetLAST_ReturnsAngleInRange()
    {
        var svc = CreateService();
        double last = svc.GetLAST();
        Assert.InRange(last, 0.0, 360.0);
    }
}
