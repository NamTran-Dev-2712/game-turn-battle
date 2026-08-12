using System.Text.Json;

namespace GameTeam.Codegen;

/// <summary>Kiểu GDScript đã resolve cho một node schema.</summary>
/// <param name="Annotation">Chú kiểu (vd <c>String</c>, <c>int</c>, <c>Array[int]</c>, <c>ConfigVersion</c>).</param>
/// <param name="IsPrimitive">True nếu là kiểu nội trị (String/int/float/bool/Array) — không nhận <c>null</c> khi đã typed.</param>
/// <param name="DocNote">Ghi chú kèm (vd <c>enum: Rarity (wire: string)</c>) hoặc <c>null</c>.</param>
public sealed record ResolvedType(string? Annotation, bool IsPrimitive, string? DocNote);

/// <summary>
/// Map kiểu OpenAPI (C# contract) → GDScript. Whitelist tường minh; gặp cấu trúc chưa hỗ trợ thì
/// ném <see cref="CodegenException"/> (KHÔNG bao giờ sinh model sai âm thầm). Bảng map: README shared/codegen.
/// </summary>
public static class GdTypeMapper
{
    /// <summary>
    /// Resolve kiểu nền (chưa xét nullable — nullable do người gọi xử lý dựa trên <see cref="ResolvedType.IsPrimitive"/>).
    /// </summary>
    public static ResolvedType Resolve(
        string schemaName,
        string? propName,
        JsonElement node,
        IReadOnlySet<string> enumNames,
        IReadOnlySet<string> dtoNames)
    {
        // Không hỗ trợ tổ hợp schema (oneOf/allOf/anyOf) — cần quyết định thủ công.
        foreach (string combinator in Combinators)
        {
            if (node.TryGetProperty(combinator, out _))
            {
                throw new CodegenException(schemaName, propName, $"cấu trúc OpenAPI chưa hỗ trợ '{combinator}'");
            }
        }

        // $ref → enum (int) hoặc DTO (class type).
        if (node.TryGetProperty("$ref", out JsonElement refEl))
        {
            string refName = RefName(refEl.GetString(), schemaName, propName);
            if (enumNames.Contains(refName))
            {
                return new ResolvedType("int", IsPrimitive: true, $"enum: {refName} (wire: string)");
            }

            if (dtoNames.Contains(refName))
            {
                return new ResolvedType(refName, IsPrimitive: false, DocNote: null);
            }

            throw new CodegenException(schemaName, propName, $"$ref treo tới schema không tồn tại '{refName}'");
        }

        string type = node.TryGetProperty("type", out JsonElement typeEl) && typeEl.ValueKind == JsonValueKind.String
            ? typeEl.GetString()!
            : throw new CodegenException(schemaName, propName, "thiếu 'type' và '$ref' — không xác định được kiểu");

        switch (type)
        {
            case "string":
                return new ResolvedType("String", IsPrimitive: true, DocNote: null);
            case "integer":
                return new ResolvedType("int", IsPrimitive: true, DocNote: null);
            case "number":
                return new ResolvedType("float", IsPrimitive: true, DocNote: null);
            case "boolean":
                return new ResolvedType("bool", IsPrimitive: true, DocNote: null);
            case "array":
                return ResolveArray(schemaName, propName, node, enumNames, dtoNames);
            case "object":
                // Object nội tuyến / map (additionalProperties) chưa hỗ trợ (chỉ DTO cấp components).
                throw new CodegenException(
                    schemaName,
                    propName,
                    "object nội tuyến / map (additionalProperties) chưa hỗ trợ — tách thành schema DTO ở components");
            default:
                throw new CodegenException(schemaName, propName, $"kiểu OpenAPI chưa hỗ trợ '{type}'");
        }
    }

    private static ResolvedType ResolveArray(
        string schemaName,
        string? propName,
        JsonElement node,
        IReadOnlySet<string> enumNames,
        IReadOnlySet<string> dtoNames)
    {
        if (!node.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Object)
        {
            throw new CodegenException(schemaName, propName, "array thiếu 'items' — không suy ra được kiểu phần tử");
        }

        ResolvedType element = Resolve(schemaName, propName, items, enumNames, dtoNames);

        // Array[X] cần X có chú kiểu; nếu phần tử untyped → Array không tham số.
        string annotation = element.Annotation is null ? "Array" : $"Array[{element.Annotation}]";
        return new ResolvedType(annotation, IsPrimitive: true, element.DocNote);
    }

    private static string RefName(string? reference, string schemaName, string? propName)
    {
        if (string.IsNullOrEmpty(reference))
        {
            throw new CodegenException(schemaName, propName, "$ref rỗng");
        }

        int slash = reference.LastIndexOf('/');
        return slash >= 0 ? reference[(slash + 1)..] : reference;
    }

    private static readonly string[] Combinators = ["oneOf", "allOf", "anyOf", "not"];
}
