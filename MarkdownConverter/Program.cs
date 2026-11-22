using System.Text;
using System.Text.Json;

/// <summary>명령줄 프로그램을 시작하고 종료 코드를 반환합니다.</summary>
internal static class Program
{
    /// <summary>전달된 인수를 출력하고 도움말을 표시하거나 변환을 실행합니다.</summary>
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine($"args: {JsonSerializer.Serialize(args)}");
        if (args.Contains("--help"))
        {
            Console.WriteLine(
                "MarkdownConverter [--input download] [--output PATH] [--project-names FILE] [--dry-run]");
            Console.WriteLine("Output defaults to yyyyMMdd-HHmm using the current Seoul time.");
            Console.WriteLine(
                "Project names default to project-names.json, a JSON object mapping project IDs to names.");
            return 0;
        }

        try
        {
            return await Converter.RunAsync(args);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException
                                          or ArgumentException
                                          or InvalidOperationException)
        {
            Console.Error.WriteLine(error is ArgumentException or InvalidOperationException
                ? error.Message
                : $"Conversion stopped ({error.GetType().Name}).");
            return 1;
        }
    }
}