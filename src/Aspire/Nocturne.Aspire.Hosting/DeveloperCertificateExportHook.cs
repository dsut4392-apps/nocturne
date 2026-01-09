using System.Diagnostics;

namespace Nocturne.Aspire.Hosting;

/// <summary>
/// Helper class that exports the ASP.NET Core developer certificate to PEM files
/// so that Vite dev servers can use them for HTTPS.
/// </summary>
public static class DeveloperCertificateExporter
{
    /// <summary>
    /// Gets the default certificate path (PEM file).
    /// </summary>
    public static string CertificatePath => Path.Combine(Path.GetTempPath(), "nocturne-dev.pem");

    /// <summary>
    /// Gets the default private key path.
    /// </summary>
    public static string KeyPath => Path.Combine(Path.GetTempPath(), "nocturne-dev.key");

    /// <summary>
    /// Ensures the developer certificate is exported to PEM files.
    /// Will reuse cached certificate if less than 1 day old.
    /// </summary>
    public static void EnsureCertificateExported()
    {
        var certPath = CertificatePath;
        var keyPath = KeyPath;

        // Check if certificate already exists and is recent (less than 1 day old)
        if (File.Exists(certPath) && File.Exists(keyPath))
        {
            var certInfo = new FileInfo(certPath);
            if (certInfo.CreationTime > DateTime.Now.AddDays(-1))
            {
                Console.WriteLine($"[Certificate] Using cached developer certificate at {certPath}");
                return;
            }
        }

        Console.WriteLine("[Certificate] Exporting ASP.NET Core developer certificate...");

        var psi = new ProcessStartInfo("dotnet", $"dev-certs https --export-path \"{certPath}\" --format Pem --no-password")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            var process = Process.Start(psi);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                Console.WriteLine($"[Certificate] Successfully exported to {certPath}");
            }
            else
            {
                Console.WriteLine("[Certificate] Warning: Failed to export certificate. HTTPS may not work for Vite apps.");
                Console.WriteLine("[Certificate] Run 'dotnet dev-certs https --trust' to ensure a valid certificate exists.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Certificate] Warning: Could not export certificate: {ex.Message}");
        }
    }
}
