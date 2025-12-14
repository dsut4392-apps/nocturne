# Compatibility Proxy Configuration

This document describes the configuration options for the Nocturne Compatibility Proxy service, including discrepancy forwarding and data redaction settings.

## Configuration Section

All settings are under `Parameters:CompatibilityProxy` in your `appsettings.json`:

```json
{
  "Parameters": {
    "CompatibilityProxy": {
      "NightscoutUrl": "https://your-nightscout.example.com",
      "NightscoutApiSecret": "your-api-secret",
      "TimeoutSeconds": 30,
      "RetryAttempts": 3,
      "DefaultStrategy": "Nightscout",
      "EnableDetailedLogging": false,
      "EnableCorrelationTracking": true,
      "EnableResponseCaching": false,
      "ResponseCacheTtlSeconds": 300,
      "ABTestingPercentage": 0,
      "MaxResponseSizeForComparison": 10485760,

      "Redaction": {
        "Enabled": true,
        "SensitiveFields": ["api_secret", "token", "password", "key", "secret", "authorization"],
        "ReplacementText": "[REDACTED]",
        "RedactUrlParameters": true,
        "UrlParametersToRedact": ["token", "api_secret", "secret", "key"]
      },

      "DiscrepancyForwarding": {
        "Enabled": false,
        "EndpointUrl": "https://monitor.nocturne.example.com",
        "ApiKey": "your-api-key",
        "SourceId": "my-instance",
        "MinimumSeverity": "Minor",
        "TimeoutSeconds": 10,
        "RetryAttempts": 3,
        "RetryDelayMs": 1000
      },

      "Comparison": {
        "ExcludeFields": ["timestamp", "date", "dateString", "_id", "id", "sysTime", "mills", "created_at", "updated_at"],
        "AllowSupersetResponses": true,
        "TimestampToleranceMs": 5000,
        "NumericPrecisionTolerance": 0.001,
        "NormalizeFieldOrdering": true,
        "ArrayOrderHandling": "Strict",
        "EnableDeepComparison": true
      },

      "CircuitBreaker": {
        "FailureThreshold": 5,
        "RecoveryTimeoutSeconds": 60,
        "SuccessThreshold": 3
      }
    }
  }
}
```

## Redaction Settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | bool | `true` | Enable/disable redaction of sensitive data |
| `SensitiveFields` | string[] | See above | Fields to redact from error messages |
| `ReplacementText` | string | `[REDACTED]` | Text to replace sensitive values with |
| `RedactUrlParameters` | bool | `true` | Redact sensitive URL query parameters |
| `UrlParametersToRedact` | string[] | See above | URL parameters to redact |

## Discrepancy Forwarding Settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | bool | `false` | Enable forwarding to remote endpoint |
| `EndpointUrl` | string | `""` | Remote Nocturne URL |
| `ApiKey` | string | `""` | Bearer token for authentication |
| `SourceId` | string | `""` | Instance identifier (defaults to machine name) |
| `MinimumSeverity` | enum | `Minor` | Minimum severity to forward: `Minor`, `Major`, `Critical` |
| `TimeoutSeconds` | int | `10` | HTTP request timeout |
| `RetryAttempts` | int | `3` | Number of retry attempts |
| `RetryDelayMs` | int | `1000` | Delay between retries (exponential backoff applied) |

## Response Selection Strategies

- `Nightscout` - Always return Nightscout response (default)
- `Nocturne` - Always return Nocturne response
- `Fastest` - Return the fastest response
- `Compare` - Select based on comparison results
- `ABTest` - Gradually shift traffic based on `ABTestingPercentage`
