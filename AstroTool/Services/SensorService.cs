using Microsoft.Maui.Devices.Sensors;

namespace AstroTool.Services;

/// <summary>
/// Reads accelerometer and gyroscope to compute device orientation.
/// Used for "window mode" (point phone at sky to see what's there).
/// </summary>
public class SensorService : IDisposable
{
    private bool _isRunning = false;

    // Current orientation in degrees
    public double Azimuth   { get; private set; } = 0;   // degrees from North
    public double Pitch     { get; private set; } = 0;   // tilt up/down
    public double Roll      { get; private set; } = 0;   // tilt left/right

    // Computed altitude = 90° - |Pitch| when pointing up
    public double Altitude => Math.Max(-90, Math.Min(90, 90 - Math.Abs(Pitch)));

    public event Action? OnOrientationChanged;

    public bool IsSupported =>
        Accelerometer.Default.IsSupported && Gyroscope.Default.IsSupported;

    /// <summary>
    /// Start listening to accelerometer and gyroscope.
    /// </summary>
    public void Start()
    {
        if (_isRunning || !IsSupported) return;

        try
        {
            Accelerometer.Default.ReadingChanged += OnAccelerometerChanged;
            Accelerometer.Default.Start(SensorSpeed.UI);

            Gyroscope.Default.ReadingChanged += OnGyroscopeChanged;
            Gyroscope.Default.Start(SensorSpeed.UI);

            _isRunning = true;
        }
        catch (FeatureNotSupportedException)
        {
            // Sensor not supported
        }
    }

    /// <summary>
    /// Stop sensor polling.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;

        try
        {
            Accelerometer.Default.Stop();
            Accelerometer.Default.ReadingChanged -= OnAccelerometerChanged;

            Gyroscope.Default.Stop();
            Gyroscope.Default.ReadingChanged -= OnGyroscopeChanged;
        }
        catch { /* ignore */ }

        _isRunning = false;
    }

    private void OnAccelerometerChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        var v = e.Reading.Acceleration;

        // Pitch: angle of phone relative to ground
        // When flat: z≈1, x≈0, y≈0. When upright: y≈1, z≈0.
        double pitch = Math.Atan2(-v.Y, Math.Sqrt(v.X * v.X + v.Z * v.Z)) * 180.0 / Math.PI;
        double roll  = Math.Atan2(v.X, v.Z) * 180.0 / Math.PI;

        Pitch = pitch;
        Roll  = roll;

        OnOrientationChanged?.Invoke();
    }

    private double _gyroIntegralZ = 0;

    private void OnGyroscopeChanged(object? sender, GyroscopeChangedEventArgs e)
    {
        // Integrate Z-axis angular velocity for heading (approximate)
        // This drifts over time; ideally combine with magnetometer
        _gyroIntegralZ += e.Reading.AngularVelocity.Z * (1.0 / 30.0) * 180.0 / Math.PI;
        Azimuth = ((_gyroIntegralZ % 360) + 360) % 360;
    }

    public void Dispose()
    {
        Stop();
    }
}
