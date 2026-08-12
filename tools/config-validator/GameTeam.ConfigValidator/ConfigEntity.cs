using System.Text.Json.Nodes;

namespace GameTeam.ConfigValidator;

/// <summary>
/// Một file config đã được phát hiện + parse. <see cref="Root"/> null nếu parse JSON thất bại
/// (đã ghi nhận JSON001) — khi đó bỏ qua các bước validate phía sau cho file này.
/// </summary>
/// <param name="FilePath">Đường dẫn báo cáo (tương đối thư mục làm việc).</param>
/// <param name="Type">Loại config suy ra từ thư mục.</param>
/// <param name="Root">Cây JSON đã parse (null nếu lỗi parse).</param>
public sealed record ConfigEntity(string FilePath, ConfigType Type, JsonNode? Root)
{
    /// <summary>Giá trị <c>id</c> ở gốc, hoặc null nếu thiếu / không phải chuỗi.</summary>
    public string? Id
    {
        get
        {
            if (Root is JsonObject obj &&
                obj.TryGetPropertyValue("id", out JsonNode? idNode) &&
                idNode is JsonValue value &&
                value.TryGetValue(out string? id))
            {
                return id;
            }

            return null;
        }
    }
}
