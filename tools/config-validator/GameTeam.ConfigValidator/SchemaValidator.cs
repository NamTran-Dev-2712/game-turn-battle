using System.Text.Json;
using Json.Schema;

namespace GameTeam.ConfigValidator;

/// <summary>
/// Validate một file config theo schema per-type (JSON Schema draft 2020-12).
/// Gom MỌI vi phạm (OutputFormat.List) → SCH001; không dừng ở lỗi đầu tiên.
/// </summary>
public static class SchemaValidator
{
    public static IEnumerable<ValidationError> Validate(ConfigEntity entity, SchemaSet schemas)
    {
        if (entity.Root is null)
        {
            yield break; // parse lỗi đã thành JSON001; không có gì để validate schema.
        }

        JsonElement element = JsonSerializer.SerializeToElement(entity.Root);
        JsonSchema schema = schemas.For(entity.Type);
        EvaluationResults results = schema.Evaluate(element, schemas.Options);

        if (results.IsValid)
        {
            yield break;
        }

        // Với OutputFormat.List, Details là danh sách phẳng các node kết quả; gom mọi node có Errors.
        IEnumerable<EvaluationResults> nodes = results.Details ?? [];
        foreach (EvaluationResults node in Enumerable.Repeat(results, 1).Concat(nodes))
        {
            if (node.Errors is not { Count: > 0 } errors)
            {
                continue;
            }

            string instancePath = node.InstanceLocation.ToString();
            foreach (KeyValuePair<string, string> error in errors)
            {
                yield return new ValidationError(
                    entity.FilePath,
                    instancePath,
                    ErrorCode.Sch001Schema,
                    $"{error.Key}: {error.Value}");
            }
        }
    }
}
