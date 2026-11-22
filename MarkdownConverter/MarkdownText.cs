using System.Text.RegularExpressions;

/// <summary>Markdown 제목과 링크 라벨에 사용할 텍스트를 정리합니다.</summary>
internal static class MarkdownText
{
    /// <summary>줄바꿈을 공백으로 바꾸고 Markdown 특수 문자를 이스케이프합니다.</summary>
    internal static string EscapeLabel(string label) =>
        Regex.Replace(label.ReplaceLineEndings(" "), @"[\\`*_{}\[\]()<>#!|&]", @"\$0");
}
