using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace GameTeam.Api.OpenApi;

/// <summary>
/// Force-publish các enum dùng chung (spine contract — Phase 05) vào <c>components.schemas</c> khi CHƯA có
/// DTO nào tham chiếu (vd Faction/Currency) — để client codegen (Phase 08) vẫn sinh đủ model từ
/// shared/contracts. Enum ĐÃ được DTO tham chiếu do bộ sinh OpenAPI tự tạo schema (và
/// <see cref="ContractEnumSchemaTransformer"/> làm giàu metadata) ⇒ KHÔNG thêm ở đây, tránh trùng khoá với
/// reference-transformer nội bộ. Đây là metadata contract — KHÔNG phải hiện thực nghiệp vụ.
/// </summary>
public sealed class ContractEnumsDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();

        foreach (Type enumType in ContractEnumSchema.SharedEnums)
        {
            // Enum đã được DTO tham chiếu ⇒ bộ sinh tạo schema riêng (schema transformer làm giàu). Thêm lại
            // ở đây gây trùng khoá với reference-transformer nội bộ ⇒ bỏ qua.
            if (ContractEnumSchema.IsReferencedByAnyContract(enumType))
            {
                continue;
            }

            OpenApiSchema schema = new();
            ContractEnumSchema.Apply(schema, enumType);
            document.Components.Schemas[enumType.Name] = schema;
        }

        return Task.CompletedTask;
    }
}
