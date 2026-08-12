namespace GameTeam.ConfigValidator.Tests;

/// <summary>
/// Định vị tài nguyên test: thư mục schema thật của repo + cây fixture (copy ra output).
/// Đi ngược từ thư mục assembly để tìm gốc repo (chứa shared/config-schema) → chạy được ở local &amp; CI.
/// </summary>
internal static class TestPaths
{
    private static readonly string RepoRoot = FindRepoRoot();

    /// <summary>Thư mục schema thật: <c>shared/config-schema</c>.</summary>
    public static string SchemaDir => Path.Combine(RepoRoot, "shared", "config-schema");

    /// <summary>Thư mục fixture Phase 06 (pass/fail per-type) để tái dùng làm test vector.</summary>
    public static string SharedFixtures => Path.Combine(SchemaDir, "fixtures");

    /// <summary>Một cây config-root fixture của validator (đã copy ra output).</summary>
    public static string Fixture(string scenario) =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", scenario);

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "shared", "config-schema")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Không tìm thấy gốc repo chứa shared/config-schema.");
    }
}
