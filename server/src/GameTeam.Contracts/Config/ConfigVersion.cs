namespace GameTeam.Contracts.Config;

/// <summary>
/// Phiên bản cấu hình: bundle version + schema version (độc lập nhau) — ADR-005,
/// docs/backend/api-and-versioning.md §5, docs/conventions/naming.md §12 (`config@v&lt;N&gt;`, `schema_version`).
/// </summary>
/// <param name="Bundle">Số phiên bản bundle cấu hình (`config@v&lt;N&gt;`).</param>
/// <param name="Schema">Phiên bản schema cấu hình (`schema_version`).</param>
public sealed record ConfigVersion(int Bundle, int Schema);
