namespace TakeTool.Core.Platform;

public static class UtilityPlatformDetector
{
    public static UtilityPlatform Current
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return UtilityPlatform.Windows;
            }

            if (OperatingSystem.IsMacOS())
            {
                return UtilityPlatform.MacOS;
            }

            if (OperatingSystem.IsLinux())
            {
                return UtilityPlatform.Linux;
            }

            return UtilityPlatform.None;
        }
    }

    public static bool IsSupported(UtilityPlatform supportedPlatforms)
        => (supportedPlatforms & Current) != 0;
}
