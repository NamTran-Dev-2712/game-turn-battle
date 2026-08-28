namespace GameTeam.Infrastructure.Configuration;

/// <summary>
/// Resolves the (possibly relative) config/schema roots from <see cref="ConfigServiceOptions"/> to
/// absolute paths, independent of the process working directory. A relative path is resolved against
/// the repo root — the nearest ancestor of the current directory or the app base directory that
/// contains both <c>config/</c> and <c>shared/config-schema/</c>. Absolute paths (e.g. test temp dirs)
/// pass through unchanged. This keeps the deploy-time publish working under <c>dotnet run</c>, the test
/// host, and a container image alike, without hardcoding a path.
/// </summary>
public static class ConfigPathResolver
{
    /// <summary>Resolve a config/schema root to an absolute path.</summary>
    public static string Resolve(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        string? repoRoot = FindRepoRoot();
        return repoRoot is not null
            ? Path.GetFullPath(Path.Combine(repoRoot, path))
            : Path.GetFullPath(path);
    }

    /// <summary>
    /// Report base for validator error paths: the parent of the (absolute) config dir, so reported
    /// paths read like <c>config/heroes/x.json</c>.
    /// </summary>
    public static string ReportBaseFor(string absoluteConfigRoot)
    {
        try
        {
            return Directory.GetParent(absoluteConfigRoot)?.FullName ?? Directory.GetCurrentDirectory();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return Directory.GetCurrentDirectory();
        }
    }

    private static string? FindRepoRoot()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            DirectoryInfo? dir = new(Path.GetFullPath(start));
            while (dir is not null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "shared", "config-schema"))
                    && Directory.Exists(Path.Combine(dir.FullName, "config")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }
        }

        return null;
    }
}
