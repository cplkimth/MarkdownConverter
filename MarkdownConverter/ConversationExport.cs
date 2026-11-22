using System.Text.Json;

/// <summary>내보낸 대화 JSON 파일을 찾아 대화를 순차적으로 읽습니다.</summary>
internal static class ConversationExport
{
    /// <summary>분할된 대화 파일을 이름순으로 찾고 없으면 단일 내보내기 파일을 찾습니다.</summary>
    internal static string[] FindFiles(string input)
    {
        var files = Directory.GetFiles(input, "conversations-*.json").Order(StringComparer.Ordinal).ToArray();
        if (files.Length == 0)
        {
            var singleFile = Path.Combine(input, "conversations.json");
            if (!File.Exists(singleFile))
                throw new InvalidOperationException("No conversation JSON files were found.");
            files = [singleFile];
        }

        return files;
    }

    /// <summary>JSON 배열에서 대화를 하나씩 비동기로 읽습니다.</summary>
    internal static async IAsyncEnumerable<JsonElement> ReadConversations(string path)
    {
        await using var stream = File.OpenRead(path);
        await foreach (var conversation in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(stream))
            yield return conversation;
    }
}
