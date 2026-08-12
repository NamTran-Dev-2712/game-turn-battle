using System.Text.Json;
using System.Text.Json.Nodes;

namespace GameTeam.ConfigValidator;

/// <summary>
/// Kiểm <c>schema_version</c>: hiện diện + là số nguyên + khớp phiên bản hỗ trợ hiện tại.
/// Không có cơ chế migration nào được định nghĩa (mọi schema đang ở v1) → phiên bản lạ là LỖI,
/// KHÔNG phát minh hành vi migrate (đó là phạm vi tương lai, xem shared/config-schema/_versions/).
/// </summary>
public static class VersionValidator
{
    /// <summary>Phiên bản schema per-type duy nhất được hỗ trợ hiện nay (chưa có migration).</summary>
    public const int SupportedSchemaVersion = 1;

    public static IEnumerable<ValidationError> Validate(ConfigEntity entity)
    {
        if (entity.Root is not JsonObject obj)
        {
            yield break; // parse lỗi / không phải object → JSON001 hoặc SCH001 đã phủ.
        }

        if (!obj.TryGetPropertyValue("schema_version", out JsonNode? node) || node is null)
        {
            yield return new ValidationError(
                entity.FilePath,
                "/schema_version",
                ErrorCode.Ver001MissingOrInvalid,
                "thiếu schema_version.");
            yield break;
        }

        if (node is not JsonValue value || !TryGetInt(value, out int version))
        {
            yield return new ValidationError(
                entity.FilePath,
                "/schema_version",
                ErrorCode.Ver001MissingOrInvalid,
                "schema_version phải là số nguyên.");
            yield break;
        }

        if (version != SupportedSchemaVersion)
        {
            yield return new ValidationError(
                entity.FilePath,
                "/schema_version",
                ErrorCode.Ver002Unsupported,
                $"schema_version {version} không được hỗ trợ (hiện tại: {SupportedSchemaVersion}).");
        }
    }

    private static bool TryGetInt(JsonValue value, out int result)
    {
        // Chỉ chấp nhận số nguyên thực sự (loại bỏ 1.5, "1", true...).
        if (value.TryGetValue(out int i))
        {
            result = i;
            return true;
        }

        if (value.GetValueKind() == JsonValueKind.Number &&
            value.TryGetValue(out double d) &&
            d == Math.Floor(d) &&
            !double.IsInfinity(d))
        {
            result = (int)d;
            return true;
        }

        result = 0;
        return false;
    }
}
