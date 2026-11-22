using System.Text.Json;

/// <summary>대화별 출력 경로를 정하고 이름 충돌을 해결합니다.</summary>
internal static class OutputPlanner
{
    private const string UnclassifiedFolder = "미분류";

    /// <summary>대화 ID별 상대 경로와 미등록 프로젝트 및 파일명 충돌 수를 계산합니다.</summary>
    internal static async Task<(Dictionary<string, string> Paths, int MissingProjectNames, int CollisionGroups)> BuildAsync(
        string[] files, string? projectNamesPath)
    {
        var projectFolders = ProjectFolders(ReadProjectNames(projectNamesPath));
        int missingProjectNames = 0;
        var plans = new Dictionary<string, string>(StringComparer.Ordinal);
        var names = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            await foreach (var conversation in ConversationExport.ReadConversations(file))
            {
                var id = Conversation.ConversationId(conversation);
                var projectId = conversation.String("conversation_template_id");
                var folder = UnclassifiedFolder;
                if (projectId?.StartsWith("g-p-", StringComparison.Ordinal) == true)
                {
                    if (projectFolders.TryGetValue(projectId, out var projectFolder))
                        folder = projectFolder;
                    else
                        missingProjectNames++;
                }

                var name = Path.Combine(folder, FileNames.ForConversation(conversation));
                if (!plans.TryAdd(id, name))
                    throw new InvalidOperationException($"Duplicate conversation ID: {id}.");
                if (!names.TryGetValue(name, out var matchingIds))
                    names.Add(name, matchingIds = []);
                matchingIds.Add(id);
            }
        }

        int collisionGroups = 0;
        foreach (var group in names.Values.Where(group => group.Count > 1))
        {
            collisionGroups++;
            int number = 0;
            foreach (var id in group.Order(StringComparer.Ordinal))
            {
                if (++number > 1)
                    plans[id] = $"{Path.ChangeExtension(plans[id], null)}_{number}.md";
            }
        }

        return (plans, missingProjectNames, collisionGroups);
    }

    /// <summary>프로젝트 ID와 이름을 연결하는 JSON 파일을 읽습니다.</summary>
    private static Dictionary<string, string> ReadProjectNames(string? path)
    {
        if (path is null)
        {
            path = "project-names.json";
            if (!File.Exists(path))
                return new(StringComparer.Ordinal);
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
               ?? throw new InvalidOperationException("The project names file must contain a JSON object.");
    }

    /// <summary>프로젝트마다 서로 다른 안전한 폴더명을 정합니다.</summary>
    private static Dictionary<string, string> ProjectFolders(Dictionary<string, string> projectNames)
    {
        var folders = new Dictionary<string, string>(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { UnclassifiedFolder, "assets" };
        foreach (var (id, name) in projectNames.OrderBy(project => project.Key, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;
            var baseName = FileNames.AvoidDeviceName(FileNames.SafeFilePart(name, 120, "프로젝트"));
            var folder = baseName;
            for (int number = 2; !used.Add(folder); number++)
                folder = $"{baseName}_{number}";
            folders.Add(id, folder);
        }

        return folders;
    }
}
