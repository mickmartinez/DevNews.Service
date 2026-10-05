using System.Globalization;
using System.Runtime.CompilerServices;

namespace DevNews.Service.Tests;

/// <summary>
/// Forces the invariant culture for the entire test assembly so that locale-sensitive
/// operations (e.g. formatting doubles into interpolated JSON strings used as fake HTTP
/// responses) are deterministic regardless of the host machine's OS-configured culture.
/// </summary>
internal static class TestCultureInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
}
