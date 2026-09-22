namespace Corel.Cli;

/// <summary>The resolved CorelDRAW version plus a COM connection established on first use, so
/// help, validation and the whole script preflight never attach to (or launch) CorelDRAW.</summary>
public sealed class CorelSession
{
    private readonly Func<object> _connect;
    private object? _app;

    public CorelSession(string? version, Func<object> connect)
    {
        Version = version;
        _connect = connect;
    }

    public static CorelSession Established(object app, string? version)
        => new(version, () => app) { _app = app };

    public string? Version { get; }

    public object App => _app ??= _connect();
}
