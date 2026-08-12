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

    /// <summary>Tên file GDScript = snake_case của tên schema + <c>.gd</c>.</summary>
    public static string ToFileName(string schemaName) => ToSnakeCase(schemaName) + ".gd";
}
