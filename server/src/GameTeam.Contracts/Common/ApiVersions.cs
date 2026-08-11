namespace GameTeam.Contracts.Common;

/// <summary>
/// Nguồn duy nhất cho quy ước versioning API <c>/api/v{major}/...</c>
/// (docs/backend/api-and-versioning.md §4, ADR-008, docs/conventions/naming.md §12).
/// Major hiện tại = v1. Tăng major khi có breaking change.
/// </summary>
public static class ApiVersions
{
    /// <summary>Major API hiện tại.</summary>
    public const string V1 = "v1";

    /// <summary>Tiền tố route của major hiện tại: <c>/api/v1</c>.</summary>
    public const string V1Prefix = "/api/" + V1;
}
