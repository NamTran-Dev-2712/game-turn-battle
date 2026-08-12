using Json.Schema;

namespace GameTeam.ConfigValidator;

/// <summary>
/// Nạp TẤT CẢ schema ở <c>shared/config-schema/*.schema.json</c> MỘT LẦN và (qua <see cref="JsonSchema.FromText"/>)
/// đăng ký theo <c>$id</c> vào <see cref="SchemaRegistry.Global"/> → mọi <c>$ref</c> (absolute URI
/// <c>https://game-team/schema/...</c>) giải cục bộ, KHÔNG fetch mạng.
/// <para>
/// Kết quả được MEMO theo thư mục schema: (1) đạt mục tiêu hiệu năng "nạp schema một lần";
/// (2) tránh double-register vào registry toàn cục (FromText không cho ghi đè cùng <c>$id</c>).
/// </para>
/// </summary>
public sealed class SchemaSet
{
    private static readonly Dictionary<string, SchemaSet> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object CacheLock = new();

    private readonly IReadOnlyDictionary<ConfigType, JsonSchema> _perType;

    private SchemaSet(EvaluationOptions options, IReadOnlyDictionary<ConfigType, JsonSchema> perType)
    {
        Options = options;
        _perType = perType;
    }

    /// <summary>Options dùng chung khi Evaluate: output List (gom mọi lỗi) + bỏ wrapper applicator.</summary>
    public EvaluationOptions Options { get; }

    /// <summary>Schema per-type để validate một file thuộc loại tương ứng.</summary>
    public JsonSchema For(ConfigType type) => _perType[type];

    /// <summary>
    /// Build (memo theo thư mục). Ném <see cref="InvalidOperationException"/> nếu thiếu schema per-type
    /// (Phase 06 bảo đảm đủ 8 + common + config-bundle) — đó là lỗi hạ tầng, không phải lỗi config.
    /// </summary>
    public static SchemaSet Build(string schemaDir)
    {
        string key = Path.GetFullPath(schemaDir);
        lock (CacheLock)
        {
            if (Cache.TryGetValue(key, out SchemaSet? cached))
            {
                return cached;
            }

            SchemaSet built = BuildUncached(key);
            Cache[key] = built;
            return built;
        }
    }

    private static SchemaSet BuildUncached(string schemaDir)
    {
        if (!Directory.Exists(schemaDir))
        {
            throw new InvalidOperationException($"Thư mục schema không tồn tại: {schemaDir}");
        }

        EvaluationOptions options = new()
        {
            OutputFormat = OutputFormat.List,   // gom toàn bộ lỗi, không dừng ở lỗi đầu tiên.
            IncludeApplicatorErrors = false,    // bỏ wrapper "properties: ..." cấp cha; giữ lỗi lá cụ thể.
        };

        // Parse MỌI *.schema.json (FromText tự đăng ký $id vào Global) — giữ theo tên file để map per-type.
        Dictionary<string, JsonSchema> byFileName = new(StringComparer.Ordinal);
        foreach (string schemaFile in Directory
                     .EnumerateFiles(schemaDir, "*.schema.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(static p => p, StringComparer.Ordinal))
        {
            byFileName[Path.GetFileName(schemaFile)] = JsonSchema.FromText(File.ReadAllText(schemaFile));
        }

        Dictionary<ConfigType, JsonSchema> perType = [];
        foreach (ConfigType type in Enum.GetValues<ConfigType>())
        {
            string fileName = ConfigFileMapper.SchemaFileName(type);
            if (!byFileName.TryGetValue(fileName, out JsonSchema? schema))
            {
                throw new InvalidOperationException($"Thiếu schema per-type: {fileName} trong {schemaDir}");
            }

            perType[type] = schema;
        }

        return new SchemaSet(options, perType);
    }
}
