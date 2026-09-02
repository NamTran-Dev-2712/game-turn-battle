using System.Text.Json;
using System.Text.Json.Nodes;
using GameTeam.Application.Abstractions.Configuration;
using GameTeam.Contracts.Config;

namespace GameTeam.Application.Tests.Combat;

/// <summary>
/// <see cref="IConfigProvider"/> in-memory cho test data-driven: nạp config bằng JSON <c>snake_case</c>
/// và deserialize giống <c>RuntimeConfigProvider</c> (Web defaults + SnakeCaseLower + case-insensitive).
/// Chứng minh sim đọc chỉ số từ config, đổi config ⇒ kết quả đổi — không chạm filesystem.
/// </summary>
public sealed class FakeConfigProvider : IConfigProvider
{
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private readonly Dictionary<string, Dictionary<string, JsonNode>> _data = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public ConfigVersion CurrentVersion { get; set; } = new(1, 1);

    /// <summary>Nạp một entry config từ JSON thô (snake_case).</summary>
    public FakeConfigProvider Set(string type, string id, string json)
    {
        if (!_data.TryGetValue(type, out Dictionary<string, JsonNode>? byId))
        {
            byId = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
            _data[type] = byId;
        }

        byId[id] = JsonNode.Parse(json)!;
        return this;
    }

    /// <inheritdoc/>
    public T? Get<T>(string type, string id)
        where T : class
    {
        if (_data.TryGetValue(type, out Dictionary<string, JsonNode>? byId)
            && byId.TryGetValue(id, out JsonNode? node))
        {
            return node.Deserialize<T>(ReadOptions);
        }

        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetIds(string type) =>
        _data.TryGetValue(type, out Dictionary<string, JsonNode>? byId)
            ? byId.Keys.ToList()
            : Array.Empty<string>();
}
