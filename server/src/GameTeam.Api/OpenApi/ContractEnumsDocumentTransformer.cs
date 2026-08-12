using GameTeam.Contracts.Enums;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace GameTeam.Api.OpenApi;

/// <summary>
/// Đăng ký các enum dùng chung (spine contract — Phase 05) vào <c>components.schemas</c> của OpenAPI,
/// kể cả khi CHƯA có DTO nền nào tham chiếu (hero DTO dùng chúng ở feature phase).
/// <para>
/// Lý do: enum là hợp đồng dùng chung, client codegen (Phase 08) cần model của chúng từ
/// shared/contracts. Serialize dạng CHUỖI (canonical name) khớp cấu hình JSON của Api.
/// Đây là metadata contract — KHÔNG phải hiện thực nghiệp vụ.
/// </para>
/// </summary>
public sealed class ContractEnumsDocumentTransformer : IOpenApiDocumentTransformer
{
    // Thứ tự ổn định (deterministic) để spec sinh ra không đổi vô cớ.
    private static readonly Type[] SharedEnums =
    [
        typeof(Faction),
        typeof(Class),
        typeof(Element),
        typeof(Role),
        typeof(Rarity),
        typeof(Currency),
    ];

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();

        foreach (Type enumType in SharedEnums)
        {
            // Tên canonical + giá trị số nền, giữ nguyên thứ tự khai báo (deterministic).
            string[] names = Enum.GetNames(enumType);
            int[] values = Enum.GetValuesAsUnderlyingType(enumType).Cast<object>()
                .Select(v => Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture))
                .ToArray();

            OpenApiSchema schema = new()
            {
                Type = "string",
                Description =
                    "Enum dùng chung (ổn định, chỉ thêm — additive). "
                    + "docs/backend/api-and-versioning.md §4.",
                Enum = names.Select(name => (IOpenApiAny)new OpenApiString(name)).ToList(),
                Extensions = new Dictionary<string, IOpenApiExtension>
                {
                    // x-enum-varnames + x-enum-values: mang giá trị số C# (vd Rarity=0,3,4,5 có "khoảng
                    // trống") qua spec — nguồn DUY NHẤT để codegen client (Phase 08) sinh enum GDScript
                    // KHỚP số của GameTeam.Contracts. Wire serialize vẫn là CHUỖI (JsonStringEnumConverter).
                    ["x-enum-varnames"] = new OpenApiArray(),
                    ["x-enum-values"] = new OpenApiArray(),
                },
            };

            var varnames = (OpenApiArray)schema.Extensions["x-enum-varnames"];
            var enumValues = (OpenApiArray)schema.Extensions["x-enum-values"];
            for (int i = 0; i < names.Length; i++)
            {
                varnames.Add(new OpenApiString(names[i]));
                enumValues.Add(new OpenApiInteger(values[i]));
            }

            document.Components.Schemas[enumType.Name] = schema;
        }

        return Task.CompletedTask;
    }
}
