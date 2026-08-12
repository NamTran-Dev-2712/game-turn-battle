namespace GameTeam.ConfigValidator;

/// <summary>
/// Một lỗi validate đơn lẻ. Report theo định dạng <c>file:jsonpath:CODE message</c>
/// để lập trình viên định vị &amp; sửa mà không cần đọc mã validator.
/// </summary>
/// <param name="File">Đường dẫn file (tương đối thư mục làm việc, vd <c>config/heroes/ignis.json</c>).</param>
/// <param name="Path">JSON Pointer tới vị trí lỗi (vd <c>/skills/0</c>); rỗng = gốc tài liệu.</param>
/// <param name="Code">Mã lỗi ổn định.</param>
/// <param name="Message">Giải thích người-đọc-được.</param>
public sealed record ValidationError(string File, string Path, ErrorCode Code, string Message)
{
    /// <summary>JSON Pointer hiển thị: rỗng → "/" cho dễ đọc.</summary>
    private string DisplayPath => string.IsNullOrEmpty(Path) ? "/" : Path;

    /// <summary>Dòng report: <c>file:jsonpath:CODE message</c>.</summary>
    public override string ToString() => $"{File}:{DisplayPath}:{Code.ToToken()} {Message}";
}
