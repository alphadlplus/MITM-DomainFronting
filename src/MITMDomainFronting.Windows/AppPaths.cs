namespace MITMDomainFronting.Windows;

internal static class AppPaths
{
    public static string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MITMDomainFrontingWindows");

    public static string CertDir { get; } = Path.Combine(DataDir, "cert");
    public static string ConfigDir { get; } = Path.Combine(DataDir, "config");
    public static string LogsDir { get; } = Path.Combine(DataDir, "logs");

    public static string CertificatePath => Path.Combine(CertDir, "mycert.crt");
    public static string KeyPath => Path.Combine(CertDir, "mycert.key");
    public static string RuntimeConfigPath => Path.Combine(ConfigDir, "MITM-DomainFronting.runtime.json");
    public static string ProxyBackupPath => Path.Combine(DataDir, "proxy-backup.json");
    public static string LogPath => Path.Combine(LogsDir, "xray.log");

    public static string AssetsDir => Path.Combine(AppContext.BaseDirectory, "assets");
    public static string BundledConfigPath => Path.Combine(AssetsDir, "MITM-DomainFronting.json");
    public static string BundledXrayPath => Path.Combine(AssetsDir, "xray", "xray.exe");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(CertDir);
        Directory.CreateDirectory(ConfigDir);
        Directory.CreateDirectory(LogsDir);
    }
}

