namespace GameTeam.Codegen.Tests;

/// <summary>
/// Định vị tài nguyên test: fixture mini (copy ra output) + spec THẬT của repo (integration).
/// Đi ngược từ thư mục assembly để tìm gốc repo (chứa shared/contracts/openapi.json).
/// </summary>
internal static class TestPaths
{
    private static readonly string RepoRoot = FindRepoRoot();

    /// <summary>Spec OpenAPI thật của repo: <c>shared/contracts/openapi.json</c>.</summary>
    public static string RealOpenApi => Path.Combine(RepoRoot, "shared", "contracts", "openapi.json");

    /// <summary>Một fixture OpenAPI mini đã copy ra output.</summary>
    public static string Fixture(string file) => Path.Combine(AppContext.BaseDirectory, "fixtures", file);

    public static string ReadFixture(string file) => File.ReadAllText(Fixture(file));

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "shared", "contracts", "openapi.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Không tìm thấy gốc repo chứa shared/contracts/openapi.json.");
    }
}
