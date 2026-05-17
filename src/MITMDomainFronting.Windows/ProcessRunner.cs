using System.Diagnostics;
using System.Text;

namespace MITMDomainFronting.Windows;

internal sealed record ProcessResult(int ExitCode, string Output);

internal static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        bool runAsAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = runAsAdmin,
            CreateNoWindow = !runAsAdmin,
        };

        if (runAsAdmin)
        {
            startInfo.Verb = "runas";
        }
        else
        {
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start process: {fileName}");

        if (runAsAdmin)
        {
            await process.WaitForExitAsync(cancellationToken);
            return new ProcessResult(process.ExitCode, string.Empty);
        }

        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) output.AppendLine(e.Data);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult(process.ExitCode, output.ToString());
    }
}
