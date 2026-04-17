namespace WiseApi.Client;

/// <summary>
/// Wise API environment to target. <see cref="Sandbox"/> is the default value so that
/// a zero-initialised <c>WiseClientOptions</c> (e.g. hydrated from a misconfigured binder)
/// can never silently hit production.
/// </summary>
public enum WiseEnvironment
{
    /// <summary><c>https://api.wise-sandbox.com</c> — isolated test environment.</summary>
    Sandbox,

    /// <summary><c>https://api.wise.com</c> — real money moves here.</summary>
    Production,
}

internal static class WiseEnvironmentExtensions
{
    public static Uri BaseAddress(this WiseEnvironment env) => env switch
    {
        WiseEnvironment.Production => new Uri("https://api.wise.com"),
        WiseEnvironment.Sandbox => new Uri("https://api.wise-sandbox.com"),
        _ => throw new ArgumentOutOfRangeException(nameof(env), env, "Unknown Wise environment."),
    };
}
