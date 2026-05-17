using System.Security.Cryptography.X509Certificates;

namespace MITMDomainFronting.Windows;

internal static class CertificateManager
{
    public static bool HasCertificate =>
        File.Exists(AppPaths.CertificatePath) && File.Exists(AppPaths.KeyPath);

    public static bool IsTrustedRoot
    {
        get
        {
            if (!File.Exists(AppPaths.CertificatePath))
            {
                return false;
            }

            using var certificate = new X509Certificate2(AppPaths.CertificatePath);
            return IsCertificateInRootStore(certificate, StoreLocation.CurrentUser)
                || IsCertificateInRootStore(certificate, StoreLocation.LocalMachine);
        }
    }

    private static bool IsCertificateInRootStore(X509Certificate2 certificate, StoreLocation location)
    {
        using var store = new X509Store(StoreName.Root, location);
        store.Open(OpenFlags.ReadOnly);
        return store.Certificates.Find(
            X509FindType.FindByThumbprint,
            certificate.Thumbprint,
            validOnly: false).Count > 0;
    }

    public static async Task GenerateAsync(string xrayExe, CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureDirectories();

        if (!File.Exists(xrayExe))
        {
            throw new FileNotFoundException("xray.exe was not found.", xrayExe);
        }

        var crt = AppPaths.CertificatePath;
        var key = AppPaths.KeyPath;
        if (File.Exists(crt)) File.Delete(crt);
        if (File.Exists(key)) File.Delete(key);

        var result = await ProcessRunner.RunAsync(
            xrayExe,
            "tls cert -ca -file=mycert",
            AppPaths.CertDir,
            cancellationToken: cancellationToken);

        if (result.ExitCode != 0 || !HasCertificate)
        {
            throw new InvalidOperationException(
                "Xray could not generate the local CA certificate." + Environment.NewLine + result.Output);
        }
    }

    public static async Task InstallTrustedRootAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(AppPaths.CertificatePath))
        {
            throw new FileNotFoundException("Generate the CA certificate first.", AppPaths.CertificatePath);
        }

        var result = await ProcessRunner.RunAsync(
            "certutil.exe",
            $"-addstore -f Root \"{AppPaths.CertificatePath}\"",
            runAsAdmin: true,
            cancellationToken: cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException("Windows did not install the certificate.");
        }
    }

    public static async Task RemoveTrustedRootAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(AppPaths.CertificatePath))
        {
            throw new FileNotFoundException("The local CA certificate file is missing.", AppPaths.CertificatePath);
        }

        using var certificate = new X509Certificate2(AppPaths.CertificatePath);
        var thumbprint = certificate.Thumbprint?.Replace(" ", string.Empty);
        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            throw new InvalidOperationException("Could not read the CA certificate thumbprint.");
        }

        var result = await ProcessRunner.RunAsync(
            "certutil.exe",
            $"-delstore Root {thumbprint}",
            runAsAdmin: true,
            cancellationToken: cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException("Windows did not remove the certificate.");
        }
    }
}
