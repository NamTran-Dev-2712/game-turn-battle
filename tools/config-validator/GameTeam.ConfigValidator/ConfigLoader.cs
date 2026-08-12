using System.Text.Json;
using System.Text.Json.Nodes;

namespace GameTeam.ConfigValidator;

/// <summary>Kết quả nạp cây config: các entity hợp lệ về mặt phát hiện + lỗi phát hiện/parse.</summary>
/// <param name="Entities">File config đã map được loại (Root có thể null nếu parse lỗi).</param>
/// <param name="Errors">MAP001 (không map được loại) và JSON001 (parse lỗi).</param>
public sealed record LoadedConfig(IReadOnlyList<ConfigEntity> Entities, IReadOnlyList<ValidationError> Errors);

/// <summary>
/// Duyệt <c>config/**.json</c> một lần: phân loại theo thư mục cấp 1, parse JSON.
/// Bỏ qua thư mục metadata (<c>liveops</c>, <c>_versions</c>); file ở vị trí không map được → MAP001.
/// </summary>
public static class ConfigLoader
{
    public static LoadedConfig Load(string configRoot, string reportBase)
    {
        List<ConfigEntity> entities = [];
        List<ValidationError> errors = [];

        if (!Directory.Exists(configRoot))
        {
            // Không có thư mục config = không có gì để validate (hợp lệ; vd bootstrap chưa author).
            return new LoadedConfig(entities, errors);
        }

        string fullConfigRoot = Path.GetFullPath(configRoot);

        // Duyệt xác định (sort) để report ổn định giữa các lần chạy / nền tảng.
        IEnumerable<string> files = Directory
            .EnumerateFiles(fullConfigRoot, "*.json", SearchOption.AllDirectories)
            .OrderBy(static p => p, StringComparer.Ordinal);

        foreach (string file in files)
        {
            string reportPath = ToReportPath(reportBase, file);
            string? firstSegment = FirstSegmentUnder(fullConfigRoot, file);

            if (firstSegment is null || !ConfigFileMapper.DirectoryToType.TryGetValue(firstSegment, out ConfigType type))
            {
                if (firstSegment is not null && ConfigFileMapper.SkippedDirectories.Contains(firstSegment))
                {
                    continue; // metadata dir — không validate theo per-type schema.
                }

                errors.Add(new ValidationError(
                    reportPath,
                    string.Empty,
                    ErrorCode.Map001UnknownType,
                    "file config không thuộc thư mục loại đã biết (heroes/skills/stages/gacha/shop/rewards/economy/quests)."));
                continue;
            }

            JsonNode? root = TryParse(file, out string? parseError);
            if (root is null)
            {
                errors.Add(new ValidationError(
                    reportPath,
                    string.Empty,
                    ErrorCode.Json001Parse,
                    parseError ?? "JSON không hợp lệ."));
                // Vẫn thêm entity (Root null) để giữ dấu vết loại; các bước sau tự bỏ qua.
            }

            entities.Add(new ConfigEntity(reportPath, type, root));
        }

        return new LoadedConfig(entities, errors);
    }

    /// <summary>Segment thư mục cấp 1 của <paramref name="file"/> so với <paramref name="root"/>.</summary>
    private static string? FirstSegmentUnder(string root, string file)
    {
        string relative = Path.GetRelativePath(root, file);
        string[] parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        // parts[^1] là tên file; cần ít nhất [dir, file] để có thư mục cấp 1.
        return parts.Length >= 2 ? parts[0] : null;
    }

    private static JsonNode? TryParse(string file, out string? error)
    {
        try
        {
            string text = File.ReadAllText(file);
            JsonNode? node = JsonNode.Parse(text);
            error = null;
            return node;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return null;
        }
    }

    /// <summary>Đường dẫn báo cáo tương đối <paramref name="reportBase"/> (thường là cwd = gốc repo), dùng '/'.</summary>
    private static string ToReportPath(string reportBase, string fullPath)
    {
        string relative = Path.GetRelativePath(reportBase, fullPath);
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }
}
