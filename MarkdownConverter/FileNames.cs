using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>대화와 첨부 파일에 사용할 이름을 안전한 경로 구성 요소로 만듭니다.</summary>
internal static class FileNames
{
    private static readonly Regex WindowsDeviceName = new(@"\A(?:CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])(?: *\.|\z)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Windows 예약 장치 이름 앞에 밑줄을 붙입니다.</summary>
    internal static string AvoidDeviceName(string name) => WindowsDeviceName.IsMatch(name) ? "_" + name : name;

    /// <summary>제목과 서울 기준 생성 시각으로 Markdown 파일명을 만듭니다.</summary>
    internal static string ForConversation(JsonElement conversation)
    {
        var date = Conversation.CreatedAt(conversation)
                       ?.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture)
                   ?? "00000000_000000";
        var name = $"{SafeFilePart(Conversation.Title(conversation), 120, "Untitled")}_{date}.md";
        return AvoidDeviceName(name);
    }

    /// <summary>파일명에 쓸 수 없는 문자를 바꾸고 길이를 제한합니다.</summary>
    internal static string SafeFilePart(string name, int maxLength, string fallback)
    {
        var result = new StringBuilder();
        foreach (var rune in name.EnumerateRunes())
        {
            if (result.Length + rune.Utf16SequenceLength > maxLength)
                break;
            if (rune.Value < 32 || (rune.IsAscii && "<>:\"/\\|?*".Contains((char)rune.Value)))
                result.Append('_');
            else
                result.Append(rune.ToString());
        }

        var trimmed = result.ToString().Trim().TrimEnd('.', ' ');
        return trimmed.Length == 0 ? fallback : trimmed;
    }
}
