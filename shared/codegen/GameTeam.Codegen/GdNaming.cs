using System.Text;

namespace GameTeam.Codegen;

/// <summary>
/// Quy tắc đặt tên GDScript (STYLE_GUIDE.md): class_name PascalCase (giữ nguyên tên schema),
/// biến snake_case, hằng/enum-member CONSTANT_CASE, tên file snake_case. Thuần hàm → deterministic.
/// </summary>
public static class GdNaming
{
    /// <summary>PascalCase/camelCase → snake_case (vd <c>expiresInSeconds</c> → <c>expires_in_seconds</c>).</summary>
    public static string ToSnakeCase(string name)
    {
        StringBuilder sb = new(name.Length + 8);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1])))
                {
                    sb.Append('_');
                }
                else if (i > 0 && i + 1 < name.Length && char.IsLower(name[i + 1]) && char.IsUpper(name[i - 1]))
                {
                    // Ranh giới acronym→word (vd "HTTPServer" → "http_server").
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>Tên → CONSTANT_CASE cho enum member (vd <c>None</c> → <c>NONE</c>, <c>Dps</c> → <c>DPS</c>).</summary>
    public static string ToConstantCase(string name) => ToSnakeCase(name).ToUpperInvariant();

    /// <summary>
    /// Từ khoá GDScript KHÔNG được dùng làm tên biến (vd wire <c>class</c> → biến <c>class</c> lỗi cú pháp).
    /// Danh sách bám GDScript 4.x — bổ sung additive nếu contract sinh trùng từ khoá khác.
    /// </summary>
    private static readonly HashSet<string> GdReservedWords = new(StringComparer.Ordinal)
    {
        "if", "elif", "else", "for", "while", "match", "break", "continue", "pass", "return",
        "class", "class_name", "extends", "is", "in", "as", "self", "super", "signal", "func",
        "static", "const", "enum", "var", "breakpoint", "preload", "await", "yield", "assert",
        "void", "namespace", "trait", "and", "or", "not", "true", "false", "null",
    };

    /// <summary>
    /// Tên biến GDScript từ khoá wire (camelCase → snake_case), có <b>escape từ khoá</b>: nếu trùng từ khoá
    /// GDScript thì thêm hậu tố <c>_</c> (vd <c>class</c> → <c>class_</c>) để file parse được. Chú thích
    /// <c>## wire: &lt;khoá gốc&gt;</c> vẫn giữ khoá JSON thật cho parser (Phase 15).
    /// </summary>
    public static string ToFieldName(string wireName)
    {
        string snake = ToSnakeCase(wireName);
        return GdReservedWords.Contains(snake) ? snake + "_" : snake;
    }

    /// <summary>Tên file GDScript = snake_case của tên schema + <c>.gd</c>.</summary>
    public static string ToFileName(string schemaName) => ToSnakeCase(schemaName) + ".gd";
}
