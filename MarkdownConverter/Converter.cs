/// <summary>대화 경로 계획, Markdown 변환, 결과 보고를 순서대로 실행합니다.</summary>
internal static class Converter
{
    /// <summary>옵션부터 결과 집계까지 전체 대화 변환을 실행합니다.</summary>
    internal static async Task<int> RunAsync(string[] args)
    {
        var (input, output, projectNamesPath, dryRun) = ConversionOptions.Parse(args);
        if (MarkdownFiles.ContainsPath(input, output) || MarkdownFiles.ContainsPath(output, input))
            throw new ArgumentException(
                "Input and output directories must be separate and must not contain each other.");
        MarkdownFiles.EnsureSafeOutputPath(output);
        MarkdownFiles.EnsureSafeOutputPath(Path.Combine(output, "assets"));

        var files = ConversationExport.FindFiles(input);
        var (plans, missingProjectNames, collisionGroups) = await OutputPlanner.BuildAsync(files, projectNamesPath);

        var outputFolders = plans.Values.Select(name => Path.Combine(output, Path.GetDirectoryName(name)!))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var folder in outputFolders)
            MarkdownFiles.EnsureSafeOutputPath(folder);

        var legacyFiles = MarkdownFiles.FindLegacyFiles(output, outputFolders);
        var assets = new AttachmentWriter(input, output);
        int written = 0;
        int skipped = 0;
        int renamed = 0;
        int failed = 0;
        foreach (var file in files)
        {
            await foreach (var conversation in ConversationExport.ReadConversations(file))
            {
                var id = Conversation.ConversationId(conversation);
                var target = Path.Combine(output, plans[id]);
                var folder = Path.GetDirectoryName(target)!;
                var assetLinkRoot = Path.GetRelativePath(folder, Path.Combine(output, "assets")).Replace('\\', '/');
                try
                {
                    var exists = File.Exists(target);
                    if (exists && !MarkdownFiles.HasConversationId(target, id))
                        throw new IOException("The destination belongs to another conversation.");

                    if (legacyFiles.TryGetValue(id, out var previous))
                    {
                        if (!dryRun)
                        {
                            Directory.CreateDirectory(folder);
                            MarkdownFiles.MoveLegacyFile(previous.Path, target, exists,
                                previous.AtRoot ? assetLinkRoot : null);
                        }

                        renamed++;
                        continue;
                    }

                    if (exists)
                    {
                        skipped++;
                        continue;
                    }

                    if (dryRun)
                    {
                        MarkdownRenderer.Render(conversation, id, TextWriter.Null, assets, assetLinkRoot, dryRun);
                        continue;
                    }

                    Directory.CreateDirectory(folder);
                    MarkdownFiles.WriteNewFile(target,
                        writer => MarkdownRenderer.Render(conversation, id, writer, assets, assetLinkRoot, dryRun));
                    written++;
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException
                                                  or InvalidOperationException or FormatException)
                {
                    Console.Error.WriteLine($"Conversation {id} failed ({error.GetType().Name}).");
                    failed++;
                }
            }
        }

        Console.WriteLine($"Output folder: {output}");
        Console.WriteLine($"Conversations: {plans.Count}, written: {written}, existing: {skipped}, failed: {failed}");
        if (renamed > 0)
            Console.WriteLine($"Conversation files {(dryRun ? "to move" : "moved")}: {renamed}");
        if (missingProjectNames > 0)
            Console.WriteLine($"Conversations without a known project name: {missingProjectNames}");
        Console.WriteLine($"Filename collision groups resolved with numeric suffixes: {collisionGroups}");
        Console.WriteLine(
            $"Image references {(dryRun ? "found" : "embedded")}: {assets.Images}, other attachment references: {assets.Other}, unresolved references: {assets.Missing}");
        return failed == 0 && assets.Missing == 0 ? 0 : 1;
    }
}