using System.Diagnostics;

namespace MITMDomainFronting.Windows;

internal sealed class XrayManager : IDisposable
{
    private Process? _process;

    public bool IsRunning => _process is { HasExited: false };

    public string XrayExePath => AppPaths.BundledXrayPath;

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        if (!File.Exists(XrayExePath))
        {
            throw new FileNotFoundException(
                "xray.exe is missing. Run scripts\\Prepare-Assets.ps1 first.",
                XrayExePath);
        }
        RequireXrayDataFile("geoip.dat");
        RequireXrayDataFile("geosite.dat");

        if (!CertificateManager.HasCertificate)
        {
            throw new InvalidOperationException("Generate and install the device CA first.");
        }

        ConfigBuilder.BuildRuntimeConfig();

        var logWriter = new StreamWriter(new FileStream(
            AppPaths.LogPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite))
        {
            AutoFlush = true,
        };

        var startInfo = new ProcessStartInfo
        {
            FileName = XrayExePath,
            Arguments = $"run -config \"{AppPaths.RuntimeConfigPath}\"",
            WorkingDirectory = Path.GetDirectoryName(XrayExePath) ?? AppPaths.DataDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) logWriter.WriteLine(e.Data);
        };
        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) logWriter.WriteLine(e.Data);
        };
        _process.Exited += (_, _) => logWriter.Dispose();

        if (!_process.Start())
        {
            logWriter.Dispose();
            throw new InvalidOperationException("Could not start xray.exe.");
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        Thread.Sleep(800);
        if (_process.HasExited)
        {
            var exitCode = _process.ExitCode;
            _process.Dispose();
            _process = null;
            throw new InvalidOperationException(
                $"Xray stopped immediately with exit code {exitCode}. Open the log for details: {AppPaths.LogPath}");
        }
    }

    private static void RequireXrayDataFile(string fileName)
    {
        var path = Path.Combine(AppPaths.AssetsDir, "xray", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"{fileName} is missing. Run scripts\\Prepare-Assets.ps1 again, then rebuild/publish the app.",
                path);
        }
    }

    public void Stop()
    {
        if (!IsRunning || _process is null)
        {
            return;
        }

        try
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(3000);
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    public void Dispose() => Stop();
}
