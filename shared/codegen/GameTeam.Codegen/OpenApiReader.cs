using System.Text.Json;

namespace GameTeam.Codegen;

/// <summary>
/// Đọc <c>openapi.json</c> (System.Text.Json — GIỮ THỨ TỰ khai báo) → <see cref="ContractModel"/>.
/// Phân loại schema: có <c>enum</c> + <c>type:string</c> → enum; có <c>type:object</c>/<c>properties</c> → DTO.
/// Enum PHẢI kèm <c>x-enum-values</c> (spec đã enrich ở ContractEnumsDocumentTransformer) — thiếu → fail rõ ràng.
/// </summary>
public static class OpenApiReader
{
    public static ContractModel Read(string openApiJson)
    {
        using JsonDocument doc = JsonDocument.Parse(openApiJson);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("components", out JsonElement components)
            || !components.TryGetProperty("schemas", out JsonElement schemas)
            || schemas.ValueKind != JsonValueKind.Object)
        {
            // Không có schema nào để sinh — hợp lệ nhưng rỗng.
            return new ContractModel([], []);
        }

        // Phân loại (một lượt) để biết tên enum vs DTO trước khi resolve property.
        List<(string Name, JsonElement Node)> enumNodes = [];
        List<(string Name, JsonElement Node)> dtoNodes = [];

        foreach (JsonProperty schema in schemas.EnumerateObject())
        {
            JsonElement node = schema.Value;
            bool isEnum = node.TryGetProperty("enum", out _)
                && node.TryGetProperty("type", out JsonElement t)
                && t.ValueKind == JsonValueKind.String
                && t.GetString() == "string";

            if (isEnum)
            {
                enumNodes.Add((schema.Name, node));
            }
            else if (IsObject(node))
            {
                dtoNodes.Add((schema.Name, node));
            }
            else
            {
                throw new CodegenException(schema.Name, null, "schema cấp components không phải enum(string) hay object — chưa hỗ trợ");
            }
        }

        HashSet<string> enumNames = [.. enumNodes.Select(e => e.Name)];
        HashSet<string> dtoNames = [.. dtoNodes.Select(d => d.Name)];

        List<EnumSchema> enums = [.. enumNodes.Select(e => ReadEnum(e.Name, e.Node))];
        List<DtoSchema> dtos = [.. dtoNodes.Select(d => ReadDto(d.Name, d.Node, enumNames, dtoNames))];

        return new ContractModel(enums, dtos);
    }

    private static bool IsObject(JsonElement node)
    {
        if (node.TryGetProperty("type", out JsonElement t) && t.ValueKind == JsonValueKind.String)
        {
            return t.GetString() == "object";
        }

        return node.TryGetProperty("properties", out _);
    }

    private static EnumSchema ReadEnum(string name, JsonElement node)
    {
        if (!node.TryGetProperty("x-enum-values", out JsonElement values) || values.ValueKind != JsonValueKind.Array)
        {
            throw new CodegenException(
                name,
                null,
                "enum thiếu 'x-enum-values' — spec phải được enrich bởi ContractEnumsDocumentTransformer (Phase 05)");
        }

        // Tên member: ưu tiên x-enum-varnames, fallback về mảng enum (canonical name).
        JsonElement names = node.TryGetProperty("x-enum-varnames", out JsonElement vn) && vn.ValueKind == JsonValueKind.Array
            ? vn
            : node.GetProperty("enum");

        int count = names.GetArrayLength();
        if (values.GetArrayLength() != count)
        {
            throw new CodegenException(name, null, "x-enum-varnames và x-enum-values lệch số phần tử");
        }

        List<EnumMember> members = new(count);
        for (int i = 0; i < count; i++)
        {
            string memberName = names[i].GetString()
                ?? throw new CodegenException(name, null, $"tên member enum #{i} rỗng");
            int value = values[i].GetInt32();
            members.Add(new EnumMember(memberName, value));
        }

        return new EnumSchema(name, members);
    }

    private static DtoSchema ReadDto(string name, JsonElement node, IReadOnlySet<string> enumNames, IReadOnlySet<string> dtoNames)
    {
        List<DtoProperty> props = [];

        if (node.TryGetProperty("properties", out JsonElement properties) && properties.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty prop in properties.EnumerateObject())
            {
                props.Add(ReadProperty(name, prop.Name, prop.Value, enumNames, dtoNames));
            }
        }

        return new DtoSchema(name, props);
    }

    private static DtoProperty ReadProperty(
        string schemaName,
        string wireName,
        JsonElement node,
        IReadOnlySet<string> enumNames,
        IReadOnlySet<string> dtoNames)
    {
        bool nullable = node.TryGetProperty("nullable", out JsonElement n)
            && n.ValueKind is JsonValueKind.True or JsonValueKind.False
            && n.GetBoolean();

        ResolvedType baseType = GdTypeMapper.Resolve(schemaName, wireName, node, enumNames, dtoNames);

        List<string> notes = [];
        string? annotation = baseType.Annotation;

        if (nullable)
        {
            notes.Add("nullable");

            // Biến typed kiểu nội trị (String/int/float/bool/Array) KHÔNG nhận null → hạ về untyped (Variant).
            // Kiểu class (Resource) đã nhận null sẵn → giữ nguyên chú kiểu.
            if (baseType.IsPrimitive)
            {
                annotation = null;
            }
        }

        if (baseType.DocNote is not null)
        {
            notes.Add(baseType.DocNote);
        }

        return new DtoProperty(wireName, GdNaming.ToSnakeCase(wireName), annotation, notes);
    }
}
