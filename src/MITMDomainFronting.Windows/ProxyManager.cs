using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace MITMDomainFronting.Windows;

internal sealed record ProxyBackup(int ProxyEnable, string? ProxyServer, string? ProxyOverride);

internal static class ProxyManager
{
    private const string InternetSettingsKey = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
    private const int InternetOptionSettingsChanged = 39;
    private const int InternetOptionRefresh = 37;

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey);
            return Convert.ToInt32(key?.GetValue("ProxyEnable") ?? 0) == 1;
        }
    }

    public static void Enable(string proxyServer = "127.0.0.1:10808")
    {
        AppPaths.EnsureDirectories();
        BackupCurrentSettingsOnce();

        using var key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, writable: true)
            ?? throw new InvalidOperationException("Could not open Windows Internet Settings registry key.");

        key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
        key.SetValue("ProxyServer", $"http={proxyServer};https={proxyServer}", RegistryValueKind.String);
        key.SetValue("ProxyOverride", "<local>", RegistryValueKind.String);
        RefreshWindowsProxy();
    }

    public static void RestoreOrDisable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, writable: true)
            ?? throw new InvalidOperationException("Could not open Windows Internet Settings registry key.");

        if (File.Exists(AppPaths.ProxyBackupPath))
        {
            var backup = JsonSerializer.Deserialize<ProxyBackup>(File.ReadAllText(AppPaths.ProxyBackupPath));
            if (backup is not null)
            {
                key.SetValue("ProxyEnable", backup.ProxyEnable, RegistryValueKind.DWord);
                SetOrDelete(key, "ProxyServer", backup.ProxyServer);
                SetOrDelete(key, "ProxyOverride", backup.ProxyOverride);
                File.Delete(AppPaths.ProxyBackupPath);
                RefreshWindowsProxy();
                return;
            }
        }

        key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
        RefreshWindowsProxy();
    }

    private static void BackupCurrentSettingsOnce()
    {
        if (File.Exists(AppPaths.ProxyBackupPath))
        {
            return;
        }

        using var key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey);
        var backup = new ProxyBackup(
            Convert.ToInt32(key?.GetValue("ProxyEnable") ?? 0),
            key?.GetValue("ProxyServer") as string,
            key?.GetValue("ProxyOverride") as string);

        File.WriteAllText(AppPaths.ProxyBackupPath, JsonSerializer.Serialize(backup, new JsonSerializerOptions
        {
            WriteIndented = true,
        }));
    }

    private static void SetOrDelete(RegistryKey key, string name, string? value)
    {
        if (value is null)
        {
            key.DeleteValue(name, throwOnMissingValue: false);
        }
        else
        {
            key.SetValue(name, value, RegistryValueKind.String);
        }
    }

    private static void RefreshWindowsProxy()
    {
        InternetSetOption(IntPtr.Zero, InternetOptionSettingsChanged, IntPtr.Zero, 0);
        InternetSetOption(IntPtr.Zero, InternetOptionRefresh, IntPtr.Zero, 0);
    }

    [DllImport("wininet.dll", SetLastError = true)]
    private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
}

