using System.Text.Json;

/// <summary>대화 JSON에서 선택 필드를 형식에 맞게 읽는 확장 메서드를 제공합니다.</summary>
internal static class JsonFields
{
    /// <summary>객체의 지정 필드를 반환하고 없으면 기본값을 반환합니다.</summary>
    public static JsonElement Member(this JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var property)
            ? property
            : default;

    /// <summary>지정 필드가 JSON 문자열일 때 문자열을 반환합니다.</summary>
    public static string? String(this JsonElement element, string name) =>
        element.Member(name) is { ValueKind: JsonValueKind.String } property ? property.GetString() : null;

    /// <summary>지정 필드가 JSON 배열일 때 원소를 순회합니다.</summary>
    public static IEnumerable<JsonElement> Items(this JsonElement element, string name) =>
        element.Member(name) is { ValueKind: JsonValueKind.Array } array ? array.EnumerateArray() : [];
}
