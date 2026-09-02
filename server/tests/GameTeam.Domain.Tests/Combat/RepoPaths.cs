namespace GameTeam.Domain.Tests.Combat;

/// <summary>
/// Định vị thư mục gốc repo từ thư mục chạy test (đi ngược lên đến khi thấy <c>shared/combat-vectors</c>
/// + <c>server</c>). Dùng để đọc golden vector — sim là thuần, chỉ test mới chạm filesystem.
/// </summary>
internal static class RepoPaths
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string CombatVectorsDir => Path.Combine(RepoRoot, "shared", "combat-vectors");

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "shared", "combat-vectors"))
                && Directory.Exists(Path.Combine(dir.FullName, "server")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Không tìm thấy repo root (chứa shared/combat-vectors + server).");
    }
}
