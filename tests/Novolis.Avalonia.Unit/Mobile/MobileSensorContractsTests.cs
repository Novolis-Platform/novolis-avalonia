using Novolis.Avalonia.Mobile;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Unit.Mobile;

public sealed class MobileSensorContractsTests
{
    [Test]
    public async Task Source_status_distinguishes_availability_from_detail()
    {
        var available = new MobileSourceStatus(MobileObservationStatus.Available);
        var denied = new MobileSourceStatus(
            MobileObservationStatus.PermissionDenied,
            "Location permission is required.");

        await Assert.That(available.IsAvailable).IsTrue();
        await Assert.That(denied.IsAvailable).IsFalse();
        await Assert.That(denied.Detail).IsEqualTo("Location permission is required.");
    }

    [Test]
    public async Task Location_reading_carries_only_platform_evidence()
    {
        var at = new DateTimeOffset(2026, 9, 28, 8, 4, 0, TimeSpan.FromHours(2));
        var reading = new MobileLocationReading(
            at,
            new GeoCoordinate(58.14623, 7.99517),
            18);

        await Assert.That(reading.At).IsEqualTo(at);
        await Assert.That(reading.Position.Latitude).IsEqualTo(58.14623);
        await Assert.That(reading.AccuracyMeters).IsEqualTo(18d);
    }

    [Test]
    public async Task Wifi_reading_can_report_redaction_without_fabricating_an_ssid()
    {
        var reading = new MobileWifiReading(
            DateTimeOffset.UtcNow,
            null,
            MobileObservationStatus.Redacted);

        await Assert.That(reading.ConnectedSsid).IsNull();
        await Assert.That(reading.Status).IsEqualTo(MobileObservationStatus.Redacted);
    }
}
