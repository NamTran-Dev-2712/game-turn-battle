using System.Globalization;
using GameTeam.Contracts.Enums;
using GameTeam.Contracts.Profile;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace GameTeam.Api.OpenApi;

/// <summary>
/// Helper dùng chung cho việc phát enum contract (spine Phase 05) vào OpenAPI. Nguồn DUY NHẤT để codegen
/// client (Phase 08) sinh enum GDScript KHỚP số của <c>GameTeam.Contracts</c> (vd Rarity=0,3,4,5 có "khoảng
/// trống") — mang qua spec bằng <c>x-enum-varnames</c> + <c>x-enum-values</c>; wire serialize là CHUỖI
/// (JsonStringEnumConverter).
/// <para>
/// Enum <b>được DTO tham chiếu</b> (vd HeroDefinitionDto → Class/Element/Role/Rarity) do bộ sinh OpenAPI tự
/// tạo schema ⇒ chỉ cần <b>làm giàu</b> metadata (schema transformer). Enum <b>chưa DTO nào tham chiếu</b>
/// (vd Faction/Currency) không được sinh ⇒ document transformer <b>force-publish</b> để codegen vẫn thấy đủ.
/// Tách hai đường bằng <see cref="IsReferencedByAnyContract"/> ⇒ tránh trùng khoá với reference-transformer
/// nội bộ (self-maintaining, không hardcode danh sách).
/// </para>
/// </summary>
internal static class ContractEnumSchema
{
    /// <summary>Các enum dùng chung (thứ tự ổn định để spec deterministic).</summary>
    public static readonly Type[] SharedEnums =
    [
        typeof(Faction),
        typeof(Class),
        typeof(Element),
        typeof(Role),
        typeof(Rarity),
        typeof(Currency),
    ];

    /// <summary>
    /// Ghi schema enum contract (string + tên canonical + x-enum-varnames/x-enum-values) lên
    /// <paramref name="schema"/>. Idempotent — gọi lại ghi đè cùng nội dung.
    /// </summary>
    public static void Apply(OpenApiSchema schema, Type enumType)
    {
        string[] names = Enum.GetNames(enumType);
        int[] values = Enum.GetValuesAsUnderlyingType(enumType).Cast<object>()
            .Select(v => Convert.ToInt32(v, CultureInfo.InvariantCulture))
            .ToArray();

        schema.Type = "string";
        schema.Enum = names.Select(name => (IOpenApiAny)new OpenApiString(name)).ToList();
        schema.Description =
            "Enum dùng chung (ổn định, chỉ thêm — additive). docs/backend/api-and-versioning.md §4.";

        OpenApiArray varnames = [];
        OpenApiArray enumValues = [];
        for (int i = 0; i < names.Length; i++)
        {
            varnames.Add(new OpenApiString(names[i]));
            enumValues.Add(new OpenApiInteger(values[i]));
        }

        schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        schema.Extensions["x-enum-varnames"] = varnames;
        schema.Extensions["x-enum-values"] = enumValues;
    }

    /// <summary>
    /// True nếu <paramref name="enumType"/> được BẤT KỲ DTO nào trong <c>GameTeam.Contracts</c> tham chiếu
    /// trực tiếp (thuộc tính có kiểu enum đó, kể cả nullable). Enum như thế do bộ sinh OpenAPI tự tạo schema
    /// ⇒ document transformer KHÔNG force-publish (tránh trùng khoá). Phát hiện bằng reflection ⇒ tự bảo trì
    /// khi thêm DTO mới.
    /// </summary>
    public static bool IsReferencedByAnyContract(Type enumType)
    {
        return typeof(ProfileDto).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetProperties())
            .Any(p => (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType) == enumType);
    }
}
