using System.Runtime.Versioning;

// The whole CLI only ever runs on Windows (CorelDRAW COM automation). The project
// targets plain net10.0 rather than net10.0-windows purely because PackAsTool rejects
// platform-specific TFMs (NETSDK1146) - this attribute keeps the platform-compatibility
// analyzer accurate without that TFM restriction.
[assembly: SupportedOSPlatform("windows")]
