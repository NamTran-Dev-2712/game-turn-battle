using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace GameTeam.Application.Tests.Combat;

/// <summary>
/// Guard <b>độ thuần tất định</b> của combat sim (ADR-011, combat-framework.md §4/§10/§11): quét mã nguồn
/// combat (Domain + Application) — <b>cấm</b> float/double, wall-clock (<c>DateTime</c>/<c>Stopwatch</c>),
/// RNG global (<c>System.Random</c>/<c>Random.Shared</c>/<c>new Random</c>), <c>Guid.NewGuid</c>. Bổ trợ
/// NetArchTest (EF/HTTP đã bị chặn ở tầng Domain/Application). Bỏ qua nội dung comment.
/// </summary>
public class CombatPuritySourceScanTests
{
    private static readonly (string Label, Regex Pattern)[] Forbidden =
    {
        ("float", new Regex(@"\bfloat\b", RegexOptions.Compiled)),
        ("double", new Regex(@"\bdouble\b", RegexOptions.Compiled)),
        ("DateTime", new Regex(@"\bDateTime\b", RegexOptions.Compiled)),
        ("DateTimeOffset", new Regex(@"\bDateTimeOffset\b", RegexOptions.Compiled)),
        ("Stopwatch", new Regex(@"\bStopwatch\b", RegexOptions.Compiled)),
        ("System.Random", new Regex(@"System\.Random", RegexOptions.Compiled)),
        ("Random.Shared", new Regex(@"Random\.Shared", RegexOptions.Compiled)),
        ("new Random", new Regex(@"new\s+Random\b", RegexOptions.Compiled)),
        ("Guid.NewGuid", new Regex(@"Guid\.NewGuid", RegexOptions.Compiled)),
    };

    public static IEnumerable<object[]> CombatSourceDirectories()
    {
        string root = RepoRoot();
        yield return new object[] { Path.Combine(root, "server", "src", "GameTeam.Domain", "Combat") };
        yield return new object[] { Path.Combine(root, "server", "src", "GameTeam.Application", "Combat") };
    }

    [Theory]
    [MemberData(nameof(CombatSourceDirectories))]
    public void Combat_source_has_no_forbidden_nondeterministic_constructs(string directory)
    {
        Directory.Exists(directory).Should().BeTrue($"thư mục combat phải tồn tại: {directory}");

        var violations = new List<string>();
        foreach (string file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            int lineNo = 0;
            foreach (string rawLine in File.ReadLines(file))
            {
                lineNo++;
                string code = StripComment(rawLine);
                if (code.Length == 0)
                {
                    continue;
                }

                foreach ((string label, Regex pattern) in Forbidden)
                {
                    if (pattern.IsMatch(code))
                    {
                        violations.Add($"{Path.GetFileName(file)}:{lineNo} → '{label}' in: {code.Trim()}");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "combat sim phải thuần tất định (không float/double/wall-clock/RNG global — ADR-011):\n"
            + string.Join("\n", violations));
    }

    // Cắt comment dòng (// ...); dòng doc-comment (/// ...) trở thành rỗng. Combat source không dùng block comment.
    private static string StripComment(string line)
    {
        int idx = line.IndexOf("//", StringComparison.Ordinal);
        return idx >= 0 ? line[..idx] : line;
    }

    private static string RepoRoot()
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
