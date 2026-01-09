using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;

namespace Nocturne.Aspire.Hosting;

/// <summary>
/// Extension methods for configuring developer certificate export for Aspire applications.
/// </summary>
public static class DeveloperCertificateExtensions
{
    /// <summary>
    /// Adds developer certificate export that runs before resources start.
    /// Exports the ASP.NET Core developer certificate to PEM files so Vite dev servers can use them for HTTPS.
    /// </summary>
    public static IDistributedApplicationBuilder AddDeveloperCertificateExport(
        this IDistributedApplicationBuilder builder)
    {
        builder.Eventing.Subscribe<BeforeStartEvent>((evt, ct) =>
        {
            DeveloperCertificateExporter.EnsureCertificateExported();
            return Task.CompletedTask;
        });
        return builder;
    }

    /// <summary>
    /// Configures a Vite/Node.js resource to use the exported developer certificate for HTTPS.
    /// This sets the SSL_CRT_FILE and SSL_KEY_FILE environment variables that Vite expects.
    /// </summary>
    /// <remarks>
    /// Call <see cref="AddDeveloperCertificateExport"/> on the builder before using this method
    /// to ensure the certificate is exported before resources start.
    /// </remarks>
    public static IResourceBuilder<T> WithDeveloperCertificateForVite<T>(
        this IResourceBuilder<T> resource) where T : IResourceWithEnvironment
    {
        return resource
            .WithEnvironment("SSL_CRT_FILE", DeveloperCertificateExporter.CertificatePath)
            .WithEnvironment("SSL_KEY_FILE", DeveloperCertificateExporter.KeyPath);
    }
}
