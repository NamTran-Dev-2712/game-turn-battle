namespace GameTeam.CombatBaseline;

/// <summary>
/// Dinh vi thu muc goc repo tu thu muc chay tool (di nguoc len den khi thay
/// <c>shared/combat-vectors</c> + <c>server</c>). Cung y tuong voi
/// <c>server/tests/GameTeam.Domain.Tests/Combat/RepoPaths.cs</c> — mot nguon vector duy nhat, khong copy.
/// </summary>
public static class RepoPaths
{
    /// <summary>Goc repo (chua <c>shared/combat-vectors</c> + <c>server</c>).</summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    /// <summary>Thu muc golden vector dung chung ca hai hien thuc (server .NET + client GDScript).</summary>
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

        throw new DirectoryNotFoundException(
            "Khong tim thay repo root (chua shared/combat-vectors + server).");
    }
}
