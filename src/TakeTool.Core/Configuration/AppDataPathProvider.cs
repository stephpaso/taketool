namespace TakeTool.Core.Configuration;

public sealed class AppDataPathProvider : IAppDataPathProvider
{
    private const string AppFolderName = "TakeTool";

    public string GetRootDirectory()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config");
        }

        return Path.Combine(root, AppFolderName);
    }
}
