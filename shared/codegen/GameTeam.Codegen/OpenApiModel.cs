namespace GameTeam.Codegen;

/// <summary>Model contract đã parse từ OpenAPI (chỉ phần cần cho codegen). Thứ tự giữ nguyên như spec.</summary>
public sealed record ContractModel(
    IReadOnlyList<EnumSchema> Enums,
    IReadOnlyList<DtoSchema> Dtos);

/// <summary>Enum dùng chung: tên + danh sách member (tên canonical + giá trị số từ x-enum-values).</summary>
public sealed record EnumSchema(string Name, IReadOnlyList<EnumMember> Members);

/// <summary>Một member enum: <c>Name</c> (canonical) + <c>Value</c> (số nền, giữ đúng "khoảng trống" C#).</summary>
public sealed record EnumMember(string Name, int Value);

/// <summary>DTO nền: tên + danh sách property (theo thứ tự khai báo trong spec).</summary>
public sealed record DtoSchema(string Name, IReadOnlyList<DtoProperty> Properties);

/// <summary>
/// Một property DTO đã map sang GDScript.
/// <param name="WireName">Khoá JSON trên dây (camelCase) — dùng cho parse ở Phase 15.</param>
/// <param name="GdName">Tên biến GDScript (snake_case).</param>
/// <param name="Annotation">Kiểu GDScript (vd <c>String</c>, <c>int</c>, <c>ConfigVersion</c>). <c>null</c> = untyped (Variant).</param>
/// <param name="DocNotes">Ghi chú (## …): nullable / enum:Name (wire: string) …</param>
/// </summary>
public sealed record DtoProperty(
    string WireName,
    string GdName,
    string? Annotation,
    IReadOnlyList<string> DocNotes);
