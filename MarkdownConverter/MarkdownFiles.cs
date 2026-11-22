using System.Text;
using System.Text.RegularExpressions;

/// <summary>Markdown 출력 경로를 검증하고 파일을 생성하거나 이전합니다.</summary>
internal static class MarkdownFiles
{
    private static readonly Regex LegacyAssetLink = new(@"(?m)^(\[[^\r\n]*\]\()assets/(?=file[_-][^\s)]+\)\r?$)");

    private static readonly Regex MinuteFileName = new(@"_[0-9]{8}_[0-9]{2}_[0-9]{2}(?:_[0-9]+)?\.md\z",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>한 경로가 다른 경로와 같거나 그 아래에 있는지 확인합니다.</summary>
    internal static bool ContainsPath(string parent, string child)
    {
        var relative = Path.GetRelativePath(parent, child);
        return !Path.IsPathRooted(relative) && relative != ".." &&
               !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    /// <summary>출력 경로와 상위 폴더에 디렉터리 링크가 없는지 확인합니다.</summary>
    internal static void EnsureSafeOutputPath(string path)
    {
        for (DirectoryInfo? directory = new(path); directory is not null; directory = directory.Parent)
        {
            if (directory.Exists && directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new ArgumentException("The output path must not contain a directory link.");
        }
    }

    /// <summary>Markdown 첫 줄에 기록할 대화 ID 주석을 만듭니다.</summary>
    internal static string IdMarker(string id) => $"<!-- Conversation-ID: {id} -->";

    /// <summary>Markdown 첫 줄의 대화 ID가 일치하는지 확인합니다.</summary>
    internal static bool HasConversationId(string path, string id)
    {
        using var reader = File.OpenText(path);
        return reader.ReadLine() == IdMarker(id);
    }

    /// <summary>출력 루트와 대화 폴더에서 이전 형식의 Markdown을 찾습니다.</summary>
    internal static Dictionary<string, (string Path, bool AtRoot)> FindLegacyFiles(
        string output, IEnumerable<string> outputFolders)
    {
        var files = new Dictionary<string, (string Path, bool AtRoot)>(StringComparer.Ordinal);
        FindInFolder(output, atRoot: true, files);
        foreach (var folder in outputFolders)
            FindInFolder(folder, atRoot: false, files);
        return files;
    }

    /// <summary>폴더에서 이전 형식 파일을 찾아 첫 줄의 대화 ID로 등록합니다.</summary>
    private static void FindInFolder(
        string folder, bool atRoot, Dictionary<string, (string Path, bool AtRoot)> files)
    {
        if (!Directory.Exists(folder))
            return;

        const string marker = "<!-- Conversation-ID: ";
        foreach (var path in Directory.EnumerateFiles(folder, "*.md"))
        {
            var name = Path.GetFileName(path);
            if ((!atRoot && !MinuteFileName.IsMatch(name)) ||
                File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                continue;
            using var reader = File.OpenText(path);
            var firstLine = reader.ReadLine();
            if (firstLine is null || !firstLine.StartsWith(marker, StringComparison.Ordinal) ||
                !firstLine.EndsWith(" -->", StringComparison.Ordinal))
                continue;

            var id = firstLine[marker.Length..^4];
            if (Conversation.IsValidId(id) &&
                (!atRoot || Path.GetFileNameWithoutExtension(name).EndsWith($"_{id}", StringComparison.Ordinal)) &&
                !files.TryAdd(id, (path, atRoot)))
                throw new InvalidOperationException(
                    "More than one legacy Markdown file has the same conversation ID.");
        }
    }

    /// <summary>기존 Markdown을 새 경로로 옮기고 필요하면 첨부 링크를 수정합니다.</summary>
    internal static void MoveLegacyFile(string source, string target, bool targetExists, string? assetLinkRoot)
    {
        if (!targetExists && assetLinkRoot is null)
        {
            File.Move(source, target);
            return;
        }

        var original = File.ReadAllText(source);
        var updated = assetLinkRoot is null
            ? original
            : LegacyAssetLink.Replace(original, match => $"{match.Groups[1].Value}{assetLinkRoot}/");
        if (!targetExists && updated == original)
        {
            File.Move(source, target);
            return;
        }

        if (targetExists)
        {
            if (File.ReadAllText(target) != updated)
                throw new IOException("The destination content differs from the legacy Markdown file.");
        }
        else
        {
            WriteNewFile(target, writer => writer.Write(updated));
        }

        File.Delete(source);
    }

    /// <summary>임시 파일에 UTF-8 Markdown을 기록한 뒤 최종 경로로 옮깁니다.</summary>
    internal static void WriteNewFile(string target, Action<TextWriter> render)
    {
        var temporary = Path.Combine(Path.GetDirectoryName(target)!, $".{Guid.NewGuid():N}.tmp");
        try
        {
            using (var writer =
                   new StreamWriter(new FileStream(temporary, FileMode.CreateNew), new UTF8Encoding(false)))
            {
                writer.NewLine = "\n";
                render(writer);
            }

            File.Move(temporary, target);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
