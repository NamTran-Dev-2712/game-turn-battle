using System.Globalization;
using System.Text.Json;

namespace GameTeam.Domain.Tests.Combat;

/// <summary>
/// So khớp <b>cấu trúc</b> hai JSON: object so theo tập khoá (không phụ thuộc thứ tự khoá), array so
/// theo phần tử (có thứ tự — event_log là dãy có nghĩa), số so như số nguyên, chuỗi/bool/null so trực
/// tiếp. Trả về đường dẫn khác biệt đầu tiên (hoặc <c>null</c> nếu trùng khớp) để test báo lỗi rõ ràng.
/// </summary>
internal static class JsonStructuralComparer
{
    public static string? FirstDifference(JsonElement expected, JsonElement actual, string path = "$")
    {
        if (expected.ValueKind != actual.ValueKind)
        {
            // Số có thể là Number cả hai; nếu khác kind thực sự khác nhau.
            return $"{path}: kind expected={expected.ValueKind} actual={actual.ValueKind}";
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                return CompareObjects(expected, actual, path);
            case JsonValueKind.Array:
                return CompareArrays(expected, actual, path);
            case JsonValueKind.Number:
                long e = expected.GetInt64();
                long a = actual.GetInt64();
                return e == a ? null : $"{path}: number expected={e} actual={a}";
            case JsonValueKind.String:
                string es = expected.GetString()!;
                string as1 = actual.GetString()!;
                return string.Equals(es, as1, StringComparison.Ordinal)
                    ? null
                    : $"{path}: string expected='{es}' actual='{as1}'";
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                return null; // kind đã khớp
            default:
                return $"{path}: unsupported kind {expected.ValueKind}";
        }
    }

    private static string? CompareObjects(JsonElement expected, JsonElement actual, string path)
    {
        var expectedProps = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (JsonProperty p in expected.EnumerateObject())
        {
            expectedProps[p.Name] = p.Value;
        }

        var actualProps = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (JsonProperty p in actual.EnumerateObject())
        {
            actualProps[p.Name] = p.Value;
        }

        foreach (string key in expectedProps.Keys)
        {
            if (!actualProps.ContainsKey(key))
            {
                return $"{path}.{key}: missing in actual";
            }
        }

        foreach (string key in actualProps.Keys)
        {
            if (!expectedProps.ContainsKey(key))
            {
                return $"{path}.{key}: unexpected in actual (value={actualProps[key].ToString()})";
            }
        }

        foreach (KeyValuePair<string, JsonElement> kv in expectedProps)
        {
            string? diff = FirstDifference(kv.Value, actualProps[kv.Key], $"{path}.{kv.Key}");
            if (diff is not null)
            {
                return diff;
            }
        }

        return null;
    }

    private static string? CompareArrays(JsonElement expected, JsonElement actual, string path)
    {
        int ec = expected.GetArrayLength();
        int ac = actual.GetArrayLength();
        if (ec != ac)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{path}: array length expected={ec} actual={ac}");
        }

        int i = 0;
        JsonElement.ArrayEnumerator ee = expected.EnumerateArray();
        JsonElement.ArrayEnumerator ae = actual.EnumerateArray();
        while (ee.MoveNext() && ae.MoveNext())
        {
            string? diff = FirstDifference(ee.Current, ae.Current, $"{path}[{i}]");
            if (diff is not null)
            {
                return diff;
            }

            i++;
        }

        return null;
    }
}
