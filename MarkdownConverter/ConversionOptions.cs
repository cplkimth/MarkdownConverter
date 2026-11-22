/// <summary>변환에 사용할 명령줄 옵션과 기본 경로를 보관합니다.</summary>
internal sealed record ConversionOptions(string Input, string Output, string? ProjectNamesPath, bool DryRun)
{
    /// <summary>명령줄 옵션을 읽고 입력 및 출력 경로를 절대 경로로 변환합니다.</summary>
    internal static ConversionOptions Parse(string[] args)
    {
        string input = "download";
        string? output = null;
        string? projectNamesPath = null;
        bool dryRun = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--input" when i + 1 < args.Length && !args[i + 1].StartsWith("--"):
                    input = args[++i];
                    break;
                case "--output" when i + 1 < args.Length && !args[i + 1].StartsWith("--"):
                    output = args[++i];
                    break;
                case "--project-names" when i + 1 < args.Length && !args[i + 1].StartsWith("--"):
                    projectNamesPath = args[++i];
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                default:
                    throw new ArgumentException("Use --help for the supported options.");
            }
        }

        input = Path.GetFullPath(input);
        output = Path.GetFullPath(output ?? TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Conversation.Seoul)
            .ToString("yyyyMMdd-HHmm", System.Globalization.CultureInfo.InvariantCulture));
        return new ConversionOptions(input, output, projectNamesPath, dryRun);
    }
}
