# Server / Infrastructure

> **Status:** Implemented - local startup integration for SeaweedFS.

## Purpose

Hosts presentation-layer infrastructure helpers that are not application or persistence features.
The local SeaweedFS hosted service starts the configured Weed binary together with `dotnet run`.

## Contents

| File | Purpose |
| --- | --- |
| `SeaweedFsOptions.cs` | Strongly typed binding for `Drive:SeaweedFs` startup settings. |
| `SeaweedFsHostedService.cs` | Starts SeaweedFS in Development when enabled, reuses an existing S3 endpoint, waits for port readiness, and stops a process owned by the API. |
| `SeaweedFsServiceCollectionExtensions.cs` | Registers the options and hosted service with ASP.NET Core dependency injection. |

## Configuration

Development settings live in `appsettings.Development.json`:

```json
"Drive": {
  "SeaweedFs": {
    "Enabled": true,
    "ExecutablePath": "C:\\windows_amd64\\weed.exe",
    "DataDirectory": "C:\\windows_amd64\\data\\institutionaldiwan",
    "S3Port": 8333,
    "FilerPort": 8888,
    "MasterPort": 9333,
    "VolumePort": 8080
  }
}
```

The service does not start in production by default because `appsettings.json` sets `Enabled=false`.
Set an explicit executable path and data directory in Development or another environment only where
SeaweedFS must be launched by the API. If port 8333 is already occupied by an existing process, the
API reuses it and does not terminate that process.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-21 | Copilot | Added automatic local SeaweedFS startup for `dotnet run`, with configurable executable/data paths and a hosted-service lifecycle. |
