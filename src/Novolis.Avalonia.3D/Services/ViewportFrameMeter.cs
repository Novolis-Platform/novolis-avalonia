namespace Novolis.Avalonia._3D.Services;

/// <summary>Rolling present-time stats for a single viewport backend.</summary>
public sealed class ViewportFrameMeter
{
    private const int Window = 90;
    private readonly double[] _samples = new double[Window];
    private readonly double[] _orbitSamples = new double[Window];
    private int _count;
    private int _index;
    private int _orbitCount;
    private int _orbitIndex;
    private double _sum;
    private double _orbitSum;

    /// <summary>Most recent present duration in milliseconds.</summary>
    public double LastMs { get; private set; }

    /// <summary>Peak present duration in the rolling window.</summary>
    public double MaxMs { get; private set; }

    /// <summary>Average present duration over the rolling window.</summary>
    public double AvgMs => _count == 0 ? 0 : _sum / _count;

    /// <summary>Implied FPS from <see cref="AvgMs"/>.</summary>
    public double Fps => AvgMs <= 1e-6 ? 0 : 1000.0 / AvgMs;

    /// <summary>Average present ms while the shared camera was interacting (orbit/zoom).</summary>
    public double OrbitAvgMs => _orbitCount == 0 ? 0 : _orbitSum / _orbitCount;

    /// <summary>Peak present ms while the camera was interacting.</summary>
    public double OrbitMaxMs { get; private set; }

    /// <summary>Samples recorded while camera was moving.</summary>
    public int OrbitSampleCount => _orbitCount;

    /// <summary>Total presents recorded.</summary>
    public int SampleCount => _count;

    /// <summary>Records one present. Pass <paramref name="cameraMoving"/> when orbit/zoom is active.</summary>
    public void Record(double presentMs, bool cameraMoving)
    {
        if (presentMs < 0 || double.IsNaN(presentMs) || double.IsInfinity(presentMs))
            return;

        LastMs = presentMs;
        if (presentMs > MaxMs)
            MaxMs = presentMs;

        if (_count == Window)
            _sum -= _samples[_index];
        else
            _count++;

        _samples[_index] = presentMs;
        _sum += presentMs;
        _index = (_index + 1) % Window;

        if (!cameraMoving)
            return;

        if (presentMs > OrbitMaxMs)
            OrbitMaxMs = presentMs;

        if (_orbitCount == Window)
            _orbitSum -= _orbitSamples[_orbitIndex];
        else
            _orbitCount++;

        _orbitSamples[_orbitIndex] = presentMs;
        _orbitSum += presentMs;
        _orbitIndex = (_orbitIndex + 1) % Window;
    }

    /// <summary>Clears rolling stats (keeps device identity elsewhere).</summary>
    public void Reset()
    {
        Array.Clear(_samples);
        Array.Clear(_orbitSamples);
        _count = _index = 0;
        _orbitCount = _orbitIndex = 0;
        _sum = _orbitSum = 0;
        LastMs = MaxMs = OrbitMaxMs = 0;
    }

    /// <summary>One-line HUD fragment.</summary>
    public string FormatLine(string name) =>
        $"{name}: {LastMs:0.0}ms  avg {AvgMs:0.0}  max {MaxMs:0.0}  ~{Fps:0}fps" +
        (_orbitCount > 0 ? $"  | orbit avg {OrbitAvgMs:0.0} max {OrbitMaxMs:0.0}" : string.Empty);
}
