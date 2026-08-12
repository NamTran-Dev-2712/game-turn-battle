namespace GameTeam.Codegen;

/// <summary>
/// Lỗi codegen do CONTRACT chứa cấu trúc chưa hỗ trợ (vd oneOf/allOf/anyOf, map/additionalProperties,
/// array thiếu items, kiểu lạ, $ref treo, enum thiếu x-enum-values). Message dạng
/// <c>schema:property:reason</c> để chỉ đúng nơi cần sửa. Generator FAIL RÕ RÀNG thay vì sinh model sai.
/// </summary>
public sealed class CodegenException : Exception
{
    public CodegenException(string schema, string? property, string reason)
        : base($"{schema}:{property ?? "-"}:{reason}")
    {
        Schema = schema;
        Property = property;
        Reason = reason;
    }

    public string Schema { get; }

    public string? Property { get; }

    public string Reason { get; }
}
