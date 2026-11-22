using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>첨부 파일을 찾아 이미지를 포함하거나 상대 링크로 기록합니다.</summary>
internal sealed class AttachmentWriter(string input, string output)
{
    private static readonly Regex AssetName = new(@"\Afile(?:_|-)[a-zA-Z0-9_-]+\.dat\z");

    private readonly Dictionary<string, string> index = ReadIndex(input);

    public int Images { get; private set; }
    public int Other { get; private set; }
    public int Missing { get; private set; }

    /// <summary>내보내기 첨부 참조에서 검증된 DAT 파일명을 추출합니다.</summary>
    public static string? Key(string? reference)
    {
        if (string.IsNullOrEmpty(reference))
            return null;
        int separator = reference.IndexOf("://", StringComparison.Ordinal);
        var name = separator >= 0 ? reference[(separator + 3)..] : reference;
        if (!name.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
            name += ".dat";
        return AssetName.IsMatch(name) ? name : null;
    }

    /// <summary>내보내기의 첨부 파일 표시 이름 목록을 읽습니다.</summary>
    private static Dictionary<string, string> ReadIndex(string input)
    {
        var path = Path.Combine(input, "conversation_asset_file_names.json");
        return File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllBytes(path)) ?? []
            : [];
    }

    /// <summary>첨부를 이미지나 파일 링크로 출력하고 처리 결과를 집계합니다.</summary>
    public void Write(TextWriter writer, string? key, JsonElement attachment, string? type, string assetLinkRoot,
        bool dryRun)
    {
        var source = key is not null ? Path.Combine(input, key) : null;
        if (source is null || !File.Exists(source))
        {
            Missing++;
            writer.WriteLine("[첨부 파일을 찾을 수 없음]");
            writer.WriteLine();
            return;
        }

        var display = index.GetValueOrDefault(key!) ?? attachment.String("name") ?? key!;
        var declaredMime = attachment.String("mime_type");
        var mime = DetectImage(source);
        if (mime is not null)
        {
            Images++;
            if (!dryRun)
            {
                writer.Write($"![{MarkdownText.EscapeLabel(display)}](data:{mime};base64,");
                writer.Write(Convert.ToBase64String(File.ReadAllBytes(source)));
                writer.WriteLine(")");
                writer.WriteLine();
            }

            return;
        }

        if (type == "image_asset_pointer" ||
            declaredMime?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
        {
            Missing++;
            writer.WriteLine("[이미지 형식을 확인할 수 없음]");
            writer.WriteLine();
            return;
        }

        Other++;
        if (dryRun)
            return;
        var fileName =
            $"{Path.GetFileNameWithoutExtension(key)}_{FileNames.SafeFilePart(display, 100, "attachment.dat")}";
        var folder = Path.Combine(output, "assets");
        var destination = Path.Combine(folder, fileName);
        Directory.CreateDirectory(folder);
        if (!File.Exists(destination))
        {
            var temporary = Path.Combine(folder, $".{Guid.NewGuid():N}.tmp");
            try
            {
                File.Copy(source, temporary);
                File.Move(temporary, destination);
            }
            finally
            {
                File.Delete(temporary);
            }
        }

        writer.WriteLine($"[{MarkdownText.EscapeLabel(display)}]({assetLinkRoot}/{Uri.EscapeDataString(fileName)})");
        writer.WriteLine();
    }

    /// <summary>파일 앞부분의 서명으로 지원되는 이미지 형식을 판별합니다.</summary>
    private static string? DetectImage(string source)
    {
        Span<byte> header = stackalloc byte[16];
        using var stream = File.OpenRead(source);
        ReadOnlySpan<byte> bytes = header[..stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false)];
        if (bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return "image/png";
        if (bytes.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
            return "image/jpeg";
        if (bytes.StartsWith("GIF87a"u8) || bytes.StartsWith("GIF89a"u8))
            return "image/gif";
        if (bytes.Length >= 12 && bytes.StartsWith("RIFF"u8) && bytes[8..].StartsWith("WEBP"u8))
            return "image/webp";
        if (bytes.StartsWith("BM"u8))
            return "image/bmp";
        if (bytes.Length >= 12 && bytes[4..8].SequenceEqual("ftyp"u8) &&
            (bytes[8..12].SequenceEqual("avif"u8) || bytes[8..12].SequenceEqual("avis"u8)))
            return "image/avif";
        return null;
    }
}