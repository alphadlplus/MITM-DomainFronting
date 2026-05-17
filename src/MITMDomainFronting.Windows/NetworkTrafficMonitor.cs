using System.Net.NetworkInformation;

namespace MITMDomainFronting.Windows;

internal sealed record TrafficSample(
    double DownloadBytesPerSecond,
    double UploadBytesPerSecond,
    ulong TotalDownloadedBytes,
    ulong TotalUploadedBytes);

internal sealed class NetworkTrafficMonitor
{
    private TrafficCounter _base;
    private TrafficCounter _last;
    private DateTime _lastAt;

    public void Start()
    {
        _base = Capture();
        _last = _base;
        _lastAt = DateTime.UtcNow;
    }

    public TrafficSample Sample()
    {
        var now = DateTime.UtcNow;
        var current = Capture();
        var seconds = Math.Max(0.2, (now - _lastAt).TotalSeconds);

        var downloaded = SafeDelta(current.ReceivedBytes, _last.ReceivedBytes);
        var uploaded = SafeDelta(current.SentBytes, _last.SentBytes);
        var totalDownloaded = SafeDelta(current.ReceivedBytes, _base.ReceivedBytes);
        var totalUploaded = SafeDelta(current.SentBytes, _base.SentBytes);

        _last = current;
        _lastAt = now;

        return new TrafficSample(
            downloaded / seconds,
            uploaded / seconds,
            totalDownloaded,
            totalUploaded);
    }

    private static TrafficCounter Capture()
    {
        ulong received = 0;
        ulong sent = 0;

        foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (item.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            if (item.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                continue;
            }

            var stats = item.GetIPv4Statistics();
            received += ToUnsigned(stats.BytesReceived);
            sent += ToUnsigned(stats.BytesSent);
        }

        return new TrafficCounter(received, sent);
    }

    private static ulong ToUnsigned(long value) => value > 0 ? (ulong)value : 0;

    private static ulong SafeDelta(ulong current, ulong previous) =>
        current >= previous ? current - previous : 0;

    private readonly record struct TrafficCounter(ulong ReceivedBytes, ulong SentBytes);
}

