using System.Runtime.Versioning;
using Microsoft.Win32;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// Prüft das Vorhandensein einer installierten Google-Chrome-Stable-Version unter Windows.
/// </summary>
public static class ChromeStablePreflight
{
    /// <summary>
    /// Stellt sicher, dass Google Chrome Stable installiert ist.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Wird geworfen, wenn das Betriebssystem nicht Windows ist.</exception>
    /// <exception cref="InvalidOperationException">Wird geworfen, wenn keine installierte Version gefunden wird.</exception>
    public static void EnsureIsInstalled()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Die Browsertests benötigen Windows mit Google Chrome Stable.");

        EnsureChromeStableIsInstalledWindows();
    }

    [SupportedOSPlatform("windows")]
    private static void EnsureChromeStableIsInstalledWindows()
    {
        var version = ReadChromeVersion(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Google Chrome"))
            ?? ReadChromeVersion(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Google Chrome"));
        if (string.IsNullOrWhiteSpace(version))
            throw new InvalidOperationException("Google Chrome Stable ist erforderlich; es wurde keine installierte Version gefunden.");
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadChromeVersion(RegistryKey? key)
    {
        using (key)
            return key?.GetValue("DisplayVersion") as string;
    }
}
