using System.Text;

namespace GameTeam.Codegen;

/// <summary>Tham số chạy codegen.</summary>
/// <param name="OpenApiPath">Đường dẫn <c>openapi.json</c> (mặc định CLI: <c>shared/contracts/openapi.json</c>).</param>
/// <param name="OutputDir">Thư mục sinh GDScript (mặc định CLI: <c>client/src/data/generated</c>).</param>
/// <param name="SourceLabel">Nhãn nguồn ghi vào header (mặc định = OpenApiPath chuẩn hoá POSIX, ổn định).</param>
public sealed record CodegenOptions(string OpenApiPath, string OutputDir, string? SourceLabel = null);

/// <summary>Một file GDScript đã sinh (tên + nội dung) — dùng cho cả test (thuần) lẫn ghi đĩa.</summary>
public sealed record GeneratedFile(string FileName, string Content);

/// <summary>Kết quả chạy: file đã ghi + file cũ (stale) đã xoá (theo tên, sắp xếp ổn định).</summary>
public sealed record CodegenReport(IReadOnlyList<string> Written, IReadOnlyList<string> Deleted);

/// <summary>
/// Điều phối codegen: đọc OpenAPI → sinh GDScript (enum + DTO) → ghi <c>OutputDir</c>, xoá file <c>.gd</c>
/// cũ không còn trong contract (giữ output KHỚP contract để drift-check đúng). Deterministic + idempotent.
/// </summary>
public static class CodegenRunner
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Sinh danh sách file (THUẦN, không chạm đĩa) — sắp xếp theo tên (ordinal) để ổn định.</summary>
    public static IReadOnlyList<GeneratedFile> Generate(string openApiJson, string sourceLabel)
    {
        ContractModel model = OpenApiReader.Read(openApiJson);

        List<GeneratedFile> files = [];
        foreach (EnumSchema e in model.Enums)
        {
            files.Add(new GeneratedFile(GdNaming.ToFileName(e.Name), GdEmitter.EmitEnum(e, sourceLabel)));
        }

        foreach (DtoSchema d in model.Dtos)
        {
            files.Add(new GeneratedFile(GdNaming.ToFileName(d.Name), GdEmitter.EmitDto(d, sourceLabel)));
        }

        return [.. files.OrderBy(static f => f.FileName, StringComparer.Ordinal)];
    }

    /// <summary>Chạy end-to-end: đọc file → sinh → ghi đĩa (LF, UTF-8 không BOM) → dọn file stale.</summary>
    public static CodegenReport Run(CodegenOptions options)
    {
        if (!File.Exists(options.OpenApiPath))
        {
            throw new InvalidOperationException($"Không tìm thấy OpenAPI: {options.OpenApiPath}");
        }

        string json = File.ReadAllText(options.OpenApiPath);
        string sourceLabel = options.SourceLabel ?? NormalizeSourceLabel(options.OpenApiPath);

        IReadOnlyList<GeneratedFile> generated = Generate(json, sourceLabel);
        Directory.CreateDirectory(options.OutputDir);

        HashSet<string> desired = [.. generated.Select(f => f.FileName)];

        // Dọn file .gd cũ không còn trong contract (chỉ .gd — giữ README.md/.gdignore… nếu có).
        List<string> deleted = [];
        foreach (string existing in Directory.EnumerateFiles(options.OutputDir, "*.gd"))
        {
            string fileName = Path.GetFileName(existing);
            if (!desired.Contains(fileName))
            {
                File.Delete(existing);
                deleted.Add(fileName);
            }
        }

        List<string> written = [];
        foreach (GeneratedFile file in generated)
        {
            string path = Path.Combine(options.OutputDir, file.FileName);
            File.WriteAllText(path, file.Content, Utf8NoBom);
            written.Add(file.FileName);
        }

        return new CodegenReport(
            [.. written.OrderBy(static f => f, StringComparer.Ordinal)],
            [.. deleted.OrderBy(static f => f, StringComparer.Ordinal)]);
    }

    /// <summary>
    /// Chuẩn hoá nhãn nguồn về POSIX tương đối (ổn định giữa các máy). Nếu rooted → thử relative theo CWD;
    /// còn dạng tuyệt đối/".." thì lấy phần đuôi <c>shared/contracts/...</c> nếu có, cuối cùng dùng tên file.
    /// </summary>
    internal static string NormalizeSourceLabel(string path)
    {
        string posix = path.Replace('\\', '/');
        if (!Path.IsPathRooted(path))
        {
            return posix.TrimStart('.', '/') is { Length: > 0 } rel ? rel : posix;
        }

        string relative = Path.GetRelativePath(Directory.GetCurrentDirectory(), path).Replace('\\', '/');
        if (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative))
        {
            return relative;
        }

        int idx = posix.IndexOf("shared/contracts/", StringComparison.Ordinal);
        return idx >= 0 ? posix[idx..] : Path.GetFileName(path);
    }
}
