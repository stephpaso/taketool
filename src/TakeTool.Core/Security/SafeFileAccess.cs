namespace TakeTool.Core.Security;

public static class SafeFileAccess
{
    /// <summary>
    /// Returns true when the path is a regular existing file suitable for utility input
    /// (not a directory, not a reparse point/symlink).
    /// </summary>
    public static bool IsSafeRegularFile(string? path, out string? fullPath)
    {
        fullPath = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception)
        {
            return false;
        }

        if (!File.Exists(fullPath) || Directory.Exists(fullPath))
        {
            return false;
        }

        var attrs = File.GetAttributes(fullPath);
        if ((attrs & FileAttributes.Directory) != 0)
        {
            return false;
        }

        if ((attrs & FileAttributes.ReparsePoint) != 0)
        {
            return false;
        }

        return true;
    }

    public static bool HasImageMagicBytes(string fullPath)
    {
        try
        {
            using var stream = File.OpenRead(fullPath);
            Span<byte> header = stackalloc byte[12];
            var read = stream.Read(header);
            if (read < 4)
            {
                return false;
            }

            // PNG
            if (read >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            {
                return true;
            }

            // JPEG
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                return true;
            }

            // GIF
            if (read >= 6 &&
                header[0] == (byte)'G' && header[1] == (byte)'I' && header[2] == (byte)'F' &&
                header[3] == (byte)'8' && (header[4] == (byte)'7' || header[4] == (byte)'9') &&
                header[5] == (byte)'a')
            {
                return true;
            }

            // WEBP: RIFF....WEBP
            if (read >= 12 &&
                header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
                header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P')
            {
                return true;
            }

            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
