namespace GameTeam.ConfigValidator;

/// <summary>Tham số chạy validate.</summary>
/// <param name="ConfigRoot">Thư mục config (vd <c>config</c>).</param>
/// <param name="SchemaRoot">Thư mục schema (vd <c>shared/config-schema</c>).</param>
/// <param name="ReportBase">Gốc để tính đường dẫn báo cáo (mặc định = thư mục làm việc).</param>
public sealed record ConfigValidatorOptions(string ConfigRoot, string SchemaRoot, string? ReportBase = null);

/// <summary>Kết quả validate: mọi lỗi đã gom + số file đã quét.</summary>
public sealed record ValidationReport(IReadOnlyList<ValidationError> Errors, int FilesScanned)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Điều phối validate (ranh giới tái dùng Phase 21 — Config Service gọi lại chính lớp này):
/// nạp schema MỘT LẦN → duyệt config MỘT LẦN → dựng IdIndex → với mỗi file: schema + version + reference,
/// GOM toàn bộ lỗi (không dừng ở lỗi đầu). Không Console/Exit ở đây (thuần logic).
/// </summary>
public static class ConfigValidationRunner
{
    public static ValidationReport Run(ConfigValidatorOptions options)
    {
        string reportBase = options.ReportBase ?? Directory.GetCurrentDirectory();

        // Nạp schema một lần (ném nếu hạ tầng schema thiếu — lỗi tool, không phải lỗi config).
        SchemaSet schemas = SchemaSet.Build(options.SchemaRoot);

        LoadedConfig loaded = ConfigLoader.Load(options.ConfigRoot, reportBase);
        IdIndex index = IdIndex.Build(loaded.Entities);

        List<ValidationError> errors = [.. loaded.Errors]; // MAP001 / JSON001 từ bước nạp.

        foreach (ConfigEntity entity in loaded.Entities)
        {
            errors.AddRange(SchemaValidator.Validate(entity, schemas));
            errors.AddRange(VersionValidator.Validate(entity));
            errors.AddRange(ReferenceValidator.Validate(entity, index));
        }

        // Sắp xếp xác định: file → path → code → message (report ổn định giữa các lần chạy).
        List<ValidationError> sorted = [.. errors
            .OrderBy(static e => e.File, StringComparer.Ordinal)
            .ThenBy(static e => e.Path, StringComparer.Ordinal)
            .ThenBy(static e => e.Code)
            .ThenBy(static e => e.Message, StringComparer.Ordinal)];

        return new ValidationReport(sorted, loaded.Entities.Count);
    }
}
