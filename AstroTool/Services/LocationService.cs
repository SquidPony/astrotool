using AstroTool.Core.Models;
using Microsoft.Maui.Devices.Sensors;

namespace AstroTool.Services;

/// <summary>
/// Provides GPS location via MAUI Essentials Geolocation.
/// Caches the last known location and falls back to default.
/// </summary>
public class LocationService
{
    private ObserverLocation? _cachedLocation;
    private DateTime _lastFetch = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public bool HasPermission { get; private set; } = false;
    public bool IsLoading { get; private set; } = false;
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Get current observer location, using cache if recent enough.
    /// Falls back to default (NYC) on error.
    /// </summary>
    public async Task<ObserverLocation> GetLocationAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cachedLocation != null
            && DateTime.UtcNow - _lastFetch < CacheDuration)
        {
            return _cachedLocation;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                HasPermission = false;
                ErrorMessage = "Location permission denied. Using default location.";
                _cachedLocation ??= ObserverLocation.Default;
                return _cachedLocation;
            }

            HasPermission = true;

            var request = new GeolocationRequest(GeolocationAccuracy.Medium,
                TimeSpan.FromSeconds(15));
            var location = await Geolocation.GetLocationAsync(request);

            if (location != null)
            {
                _cachedLocation = new ObserverLocation
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    AltitudeMeters = location.Altitude ?? 0,
                    Name = "Current Location"
                };
                _lastFetch = DateTime.UtcNow;
            }
            else
            {
                ErrorMessage = "Unable to get GPS fix. Using last known location.";
                _cachedLocation ??= ObserverLocation.Default;
            }
        }
        catch (FeatureNotSupportedException)
        {
            ErrorMessage = "GPS not supported on this device. Using default location.";
            _cachedLocation = ObserverLocation.Default;
        }
        catch (FeatureNotEnabledException)
        {
            ErrorMessage = "GPS is disabled. Enable location services and try again.";
            _cachedLocation ??= ObserverLocation.Default;
        }
        catch (PermissionException)
        {
            ErrorMessage = "Location permission denied.";
            _cachedLocation ??= ObserverLocation.Default;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Location error: {ex.Message}";
            _cachedLocation ??= ObserverLocation.Default;
        }
        finally
        {
            IsLoading = false;
        }

        return _cachedLocation!;
    }

    /// <summary>
    /// Manually set a custom observer location.
    /// </summary>
    public void SetLocation(double lat, double lon, double altM = 0, string name = "Custom")
    {
        _cachedLocation = new ObserverLocation
        {
            Latitude = lat,
            Longitude = lon,
            AltitudeMeters = altM,
            Name = name
        };
        _lastFetch = DateTime.UtcNow;
    }

    public ObserverLocation CurrentLocation => _cachedLocation ?? ObserverLocation.Default;
}
