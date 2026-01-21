# Aspire Publish with Runtime Configuration

## Overview

This design shifts Nocturne's deployment model from build-time configuration to runtime configuration. The `aspire publish` process generates an authoritative `docker-compose.yml` that includes all services by default. Users configure what runs via environment variables rather than editing YAML.

**Key changes:**
- Pre-built oref WASM downloaded from GitHub Releases during `dotnet restore`
- All connectors included in docker-compose, self-disable if not enabled
- Watchtower optionally included based on appsettings
- Minimal `appsettings.publish.json` (Aspire dashboard disabled, Scalar disabled, Watchtower enabled)
- Generated docker-compose is git-ignored

## Component 1: oRef Package Download

**Location:** `src/Core/Nocturne.Core.Oref/Nocturne.Core.Oref.csproj`

Add an MSBuild target that:
- Checks if `wwwroot/oref_bg.wasm` exists
- If missing, downloads from `https://github.com/nightscout/nocturne/releases/download/oref-v{version}/oref_bg.wasm`
- Version tracked in a `oref.version` file in the project
- Runs before `Build` target

**Configuration flag:** `Parameters:Oref:CompileFromSource` in appsettings
- When `true`: Aspire keeps the cargo build executable (existing behavior)
- When `false` (default): Skip cargo build, rely on downloaded package

**New file:** `src/Core/oref/README.md`
- Documents that oref is pre-compiled by default
- Instructions for local development: set `CompileFromSource: true` and ensure Rust toolchain installed

## Component 2: Connector Runtime Self-Disable

**Change location:** Each connector's Program.cs or startup logic

Add early-exit check at startup using existing convention:
```csharp
if (!builder.Configuration.GetValue<bool>("Parameters:Connectors:{Name}:Enabled", false))
{
    Console.WriteLine($"[{connectorName}] Connector not enabled. Exiting.");
    return;
}
```

**Environment variable pattern:** Each connector service in docker-compose gets the enabled flag via the existing `Parameters:Connectors:{Name}:Enabled` convention.

**Behavior:**
- Service starts, checks config, exits immediately with code 0 if not enabled
- Docker Compose sees clean exit, doesn't restart
- Users enable connectors by setting appropriate environment variables

**Source generator update:** The `ConnectorExtensionsGenerator` needs to emit the enabled environment variable for each connector in the publish output.

## Component 3: Watchtower Conditional Inclusion

**Configuration:** `Parameters:OptionalServices:Watchtower:Enabled`

**Program.cs addition:**
```csharp
var enableWatchtower = builder.Configuration.GetValue<bool>(
    "Parameters:OptionalServices:Watchtower:Enabled",
    false
);
if (enableWatchtower)
{
    builder.AddContainer("watchtower", "ghcr.io/nicholas-fedor/watchtower")
        .WithVolume("/var/run/docker.sock", "/var/run/docker.sock")
        .WithEnvironment("WATCHTOWER_CLEANUP", "true")
        .WithEnvironment("WATCHTOWER_POLL_INTERVAL", "86400")
        .PublishAsDockerComposeService((_, _) => { });
}
```

## Component 4: Configuration Structure

**appsettings.publish.json:**
```json
{
  "Parameters": {
    "OptionalServices": {
      "AspireDashboard": {
        "Enabled": false
      },
      "Scalar": {
        "Enabled": false
      },
      "Watchtower": {
        "Enabled": true
      }
    }
  }
}
```

**GenerateModels.cs refactor:**
```csharp
public class OptionalServicesConfig
{
    public OptionalServiceConfig AspireDashboard { get; set; } = new() { Enabled = true };
    public OptionalServiceConfig Scalar { get; set; } = new() { Enabled = true };
    public OptionalServiceConfig Watchtower { get; set; } = new();
}

public class OptionalServiceConfig
{
    public bool Enabled { get; set; }
}
```

**Program.cs config path changes:**
- `Parameters:IncludeDashboard` → `Parameters:OptionalServices:AspireDashboard:Enabled`
- `Parameters:IncludeScalar` → `Parameters:OptionalServices:Scalar:Enabled`

## Files to Change

**New files:**
1. `appsettings.publish.json` - Minimal publish config
2. `src/Core/oref/README.md` - Documents compile-from-source option

**Modified files:**
1. `src/Aspire/Nocturne.Aspire.Host/Program.cs`
   - Load appsettings.publish.json during publish
   - Change optional service config paths to new convention
   - Add Watchtower container conditionally
   - Make oref build conditional based on `Parameters:Oref:CompileFromSource`

2. `src/Core/Nocturne.Core.Oref/Nocturne.Core.Oref.csproj`
   - Add MSBuild target to download oref WASM from GitHub Releases

3. `src/Portal/Nocturne.Portal.API/Models/GenerateModels.cs`
   - Refactor `OptionalServicesConfig` to use nested `OptionalServiceConfig` objects

4. `src/Aspire/Nocturne.Aspire.SourceGenerators/ConnectorExtensionsGenerator.cs`
   - Ensure connectors emit enabled env var

5. Connector projects (each connector's Program.cs/startup)
   - Add early-exit check for enabled flag

6. `.gitignore`
   - Verify `aspire-output/` is ignored
