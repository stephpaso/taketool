namespace TakeTool.Core.Platform;

[Flags]
public enum UtilityPlatform
{
    None = 0,
    Windows = 1,
    MacOS = 2,
    Linux = 4,
    Any = Windows | MacOS | Linux
}
