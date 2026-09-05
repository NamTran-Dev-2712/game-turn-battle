using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace GameTeam.Api.OpenApi;

/// <summary>
/// Làm giàu schema của enum contract dùng chung KHI được DTO tham chiếu (vd HeroDefinitionDto →
/// Class/Element/Role/Rarity): gắn tên canonical + <c>x-enum-varnames</c>/<c>x-enum-values</c> để client
/// codegen (Phase 08) sinh enum GDScript KHỚP số C# (Rarity=0,3,4,5). Bộ sinh OpenAPI đã tạo schema cho enum
/// được tham chiếu; đây chỉ bổ sung metadata (không trùng khoá). Enum chưa tham chiếu do
/// <see cref="ContractEnumsDocumentTransformer"/> force-publish.
/// </summary>
public sealed class ContractEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        Type type = context.JsonTypeInfo.Type;
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying.IsEnum && Array.IndexOf(ContractEnumSchema.SharedEnums, underlying) >= 0)
        {
            ContractEnumSchema.Apply(schema, underlying);
        }

        return Task.CompletedTask;
    }
}
