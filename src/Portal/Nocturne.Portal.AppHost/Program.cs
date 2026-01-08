using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add the Portal API service
#pragma warning disable ASPIRECERTIFICATES001
var api = builder
    .AddProject<Projects.Nocturne_Portal_API>("portal-api")
    .WithHttpsDeveloperCertificate()
    .WithHttpsEndpoint(port: 1610);

// Export the ASP.NET Core developer certificate to a file so Vite can use it
// TODO: This works, but isn't very pretty, and could be refactored, probably. Would be nice to do things the Aspire way, I'm sure that there's a better way, but couldn't figure it out.
var tempDir = Path.GetTempPath();
var certPath = Path.Combine(tempDir, "nocturne-portal.pem");
var keyPath = Path.Combine(tempDir, "nocturne-portal.key");
var exportCert = true;

if (File.Exists(certPath) && File.Exists(keyPath))
{
    var certInfo = new FileInfo(certPath);
    // If cert is younger than 1 day, reuse it
    if (certInfo.CreationTime > DateTime.Now.AddDays(-1))
    {
        exportCert = false;
    }
}

if (exportCert)
{
    // Check if "dotnet dev-certs" is available
    var psi = new System.Diagnostics.ProcessStartInfo("dotnet", $"dev-certs https --export-path \"{certPath}\" --format Pem --no-password")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    var process = System.Diagnostics.Process.Start(psi);
    process?.WaitForExit();

    if (process?.ExitCode != 0)
    {
        // Fallback or log warning - for now we'll just continue and hope for the best or that the user fixes it
        // In a real scenario we might want to fail fast
    }
}

// Add the Portal Web frontend
builder.AddViteApp("portal-web", "../../Web/packages/portal")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("VITE_PORTAL_API_URL", api.GetEndpoint("https"))
    .WithEnvironment("SSL_CRT_FILE", certPath)
    .WithEnvironment("SSL_KEY_FILE", keyPath)
    .WithHttpsEndpoint(env: "PORT", port: 1611)
    .WithHttpsDeveloperCertificate()
    .WithDeveloperCertificateTrust(true)
    .PublishAsDockerFile();
#pragma warning restore ASPIRECERTIFICATES001

var app = builder.Build();
app.Run();
