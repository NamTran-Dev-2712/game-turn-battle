using System.Text.Json;

namespace GameTeam.Infrastructure.Serialization;

/// <summary>
/// Tuỳ chọn serialize JSON DÙNG CHUNG cho cache (Redis) — một nguồn duy nhất để hành vi
/// deterministic và document được. Web defaults (camelCase, case-insensitive) + converter
/// <see cref="ResultJsonConverterFactory"/> để round-trip <c>Result</c>/<c>Result&lt;T&gt;</c>
/// (xem docs/backend/infrastructure.md — Redis cache).
/// </summary>
public static class CacheSerialization
{
    /// <summary>Tuỳ chọn bất biến, chia sẻ toàn tiến trình (STJ tự cache metadata).</summary>
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new ResultJsonConverterFactory());
        // Gắn reflection-based resolver rồi đóng băng ⇒ bất biến, deterministic, thread-safe.
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
