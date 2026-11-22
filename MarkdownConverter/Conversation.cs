using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>대화 JSON에서 대화 정보와 현재 메시지 분기를 읽습니다.</summary>
internal static class Conversation
{
    private static readonly Regex SafeId = new(@"\A[a-zA-Z0-9_-]{5,128}\z");

    internal static readonly TimeZoneInfo Seoul = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");

    /// <summary>대화 ID에 허용된 문자와 길이를 검사합니다.</summary>
    internal static bool IsValidId(string id) => SafeId.IsMatch(id);

    /// <summary>대화 ID를 읽고 형식이 잘못되면 예외를 던집니다.</summary>
    internal static string ConversationId(JsonElement conversation)
    {
        var id = conversation.String("id") ?? conversation.String("conversation_id");
        return id is not null && IsValidId(id)
            ? id
            : throw new InvalidOperationException("A conversation has a missing or invalid ID.");
    }

    /// <summary>Unix 타임스탬프를 서울 시간으로 변환합니다.</summary>
    internal static DateTimeOffset? CreatedAt(JsonElement conversation)
    {
        var value = conversation.Member("create_time");
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double seconds) ||
            !double.IsFinite(seconds) || seconds is < -62135596800 or >= 253402300800)
            return null;
        return TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds((long)(seconds * 1000)), Seoul);
    }

    /// <summary>대화 제목을 읽고 제목이 비어 있으면 기본 제목을 반환합니다.</summary>
    internal static string Title(JsonElement conversation) =>
        conversation.String("title") is { } title && !string.IsNullOrWhiteSpace(title) ? title : "제목 없음";

    /// <summary>현재 메시지에서 부모를 따라가며 선택된 분기의 메시지를 시간순으로 반환합니다.</summary>
    internal static IEnumerable<JsonElement> ActiveMessages(JsonElement conversation)
    {
        var mapping = conversation.Member("mapping");
        if (mapping.ValueKind != JsonValueKind.Object || conversation.String("current_node") is not { } node)
            throw new InvalidDataException();

        var path = new Stack<JsonElement>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (node is not null)
        {
            if (!visited.Add(node) || !mapping.TryGetProperty(node, out var entry))
                throw new InvalidDataException();
            var message = entry.Member("message");
            if (message.ValueKind == JsonValueKind.Object)
                path.Push(message);
            node = entry.String("parent");
        }

        return path;
    }
}
