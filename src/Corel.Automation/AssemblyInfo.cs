using System.Runtime.Versioning;

// COM automation against CorelDRAW is inherently Windows-only. Targets plain net10.0
// (not net10.0-windows) so Corel.Cli can reference it while still being packable as a
// dotnet tool (PackAsTool rejects platform-specific TFMs) - this attribute keeps the
// platform-compatibility analyzer accurate without that TFM restriction.
[assembly: SupportedOSPlatform("windows")]
