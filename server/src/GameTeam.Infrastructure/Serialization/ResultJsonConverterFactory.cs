using System.Text.Json;
using System.Text.Json.Serialization;
using GameTeam.Domain.Common;

namespace GameTeam.Infrastructure.Serialization;

/// <summary>
/// System.Text.Json converter cho <see cref="Result"/> và <see cref="Result{T}"/>.
/// <para>
/// Vì sao cần: các kiểu <c>Result</c> (Phase 09) là bất biến — ctor không public, thuộc tính chỉ đọc —
/// nên STJ mặc định KHÔNG deserialize được. <c>CachingBehavior</c> (Phase 10) cache nguyên response
/// (vd <c>Result&lt;ServerTimeResponse&gt;</c>), nên cache Redis phải round-trip được <c>Result</c>.
/// Converter này giữ Domain sạch (không nhét attribute JSON vào Domain) — mối lo serialize nằm ở
/// Infrastructure, đúng ranh giới. Định dạng: <c>{ "isSuccess", "error": { "code", "message" }, "value"? }</c>.
/// </para>
/// </summary>
public sealed class ResultJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(Result)
           || (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Result<>));

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(Result))
        {
            return new ResultConverter();
        }

        Type valueType = typeToConvert.GetGenericArguments()[0];
        Type converterType = typeof(ResultOfTConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    // Khoá JSON cố định (không phụ thuộc naming policy) — read/write đối xứng nên luôn khớp.
    internal const string IsSuccessName = "isSuccess";
    internal const string ErrorName = "error";
    internal const string ValueName = "value";
    internal const string CodeName = "code";
    internal const string MessageName = "message";

    /// <summary>Ghi khối lỗi <c>{ "code", "message" }</c> (thủ công — không phụ thuộc record deserialization).</summary>
    internal static void WriteError(Utf8JsonWriter writer, Error error)
    {
        writer.WritePropertyName(ErrorName);
        writer.WriteStartObject();
        writer.WriteString(CodeName, error.Code);
        writer.WriteString(MessageName, error.Message);
        writer.WriteEndObject();
    }

    /// <summary>Đọc khối lỗi; trả <see cref="Error.None"/> khi thiếu hoặc rỗng (giữ bất biến Result).</summary>
    internal static Error ReadError(JsonElement root)
    {
        if (!root.TryGetProperty(ErrorName, out JsonElement errorElement)
            || errorElement.ValueKind != JsonValueKind.Object)
        {
            return Error.None;
        }

        string code = errorElement.TryGetProperty(CodeName, out JsonElement codeElement)
            ? codeElement.GetString() ?? string.Empty
            : string.Empty;
        string message = errorElement.TryGetProperty(MessageName, out JsonElement messageElement)
            ? messageElement.GetString() ?? string.Empty
            : string.Empty;

        return code.Length == 0 && message.Length == 0 ? Error.None : new Error(code, message);
    }

    internal static bool ReadIsSuccess(JsonElement root)
        => root.TryGetProperty(IsSuccessName, out JsonElement element)
           && element.ValueKind == JsonValueKind.True;
}

/// <summary>Converter cho <see cref="Result"/> không mang giá trị.</summary>
internal sealed class ResultConverter : JsonConverter<Result>
{
    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        return ResultJsonConverterFactory.ReadIsSuccess(root)
            ? Result.Success()
            : Result.Failure(ResultJsonConverterFactory.ReadError(root));
    }

    public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean(ResultJsonConverterFactory.IsSuccessName, value.IsSuccess);
        ResultJsonConverterFactory.WriteError(writer, value.Error);
        writer.WriteEndObject();
    }
}

/// <summary>Converter cho <see cref="Result{T}"/>; ghi <c>value</c> chỉ khi thành công.</summary>
internal sealed class ResultOfTConverter<TValue> : JsonConverter<Result<TValue>>
{
    public override Result<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (!ResultJsonConverterFactory.ReadIsSuccess(root))
        {
            return Result.Failure<TValue>(ResultJsonConverterFactory.ReadError(root));
        }

        TValue value = root.TryGetProperty(ResultJsonConverterFactory.ValueName, out JsonElement valueElement)
                       && valueElement.ValueKind != JsonValueKind.Null
            ? valueElement.Deserialize<TValue>(options)!
            : default!;

        return Result.Success(value);
    }

    public override void Write(Utf8JsonWriter writer, Result<TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean(ResultJsonConverterFactory.IsSuccessName, value.IsSuccess);
        ResultJsonConverterFactory.WriteError(writer, value.Error);

        if (value.IsSuccess)
        {
            writer.WritePropertyName(ResultJsonConverterFactory.ValueName);
            JsonSerializer.Serialize(writer, value.Value, options);
        }

        writer.WriteEndObject();
    }
}
