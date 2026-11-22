using System.Text.Json;

/// <summary>선택된 대화 메시지를 Markdown 문서로 작성합니다.</summary>
internal static class MarkdownRenderer
{
    /// <summary>대화 머리말과 표시 가능한 메시지의 텍스트 및 첨부를 씁니다.</summary>
    internal static void Render(JsonElement conversation, string id, TextWriter writer, AttachmentWriter assets,
        string assetLinkRoot, bool dryRun)
    {
        var messages = Conversation.ActiveMessages(conversation);
        writer.WriteLine(MarkdownFiles.IdMarker(id));
        writer.WriteLine();
        writer.WriteLine($"# {MarkdownText.EscapeLabel(Conversation.Title(conversation))}");
        writer.WriteLine();
        writer.WriteLine(
            $"- 생성일: {Conversation.CreatedAt(conversation)?.ToString("yyyy-MM-dd HH:mm:ss zzz", System.Globalization.CultureInfo.InvariantCulture) ?? "기록 없음"}");
        writer.WriteLine();

        foreach (var message in messages)
        {
            var role = message.Member("author").String("role");
            var content = message.Member("content");
            var metadata = message.Member("metadata");
            if (role is not ("user" or "assistant") ||
                message.String("recipient") is not (null or "all") ||
                metadata.Member("is_visually_hidden_from_conversation").ValueKind == JsonValueKind.True ||
                content.String("content_type") is not ("text" or "multimodal_text"))
                continue;

            var attachments = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var attachment in metadata.Items("attachments"))
            {
                if (AttachmentWriter.Key(attachment.String("id")) is { } key)
                    attachments.TryAdd(key, attachment);
            }

            var messageWriter = new MessageWriter(writer, role, assets, assetLinkRoot, dryRun);

            foreach (var part in content.Items("parts"))
            {
                if (part.ValueKind == JsonValueKind.String)
                    messageWriter.WriteText(part.GetString());
                else if (part.String("content_type") == "audio_transcription")
                    messageWriter.WriteText(part.String("text"));
                else if (part.String("asset_pointer") is { } pointer)
                {
                    var key = AttachmentWriter.Key(pointer);
                    var attachment = key is not null ? attachments.GetValueOrDefault(key) : default;
                    messageWriter.WriteAsset(key, attachment, part.String("content_type"));
                }
            }

            foreach (var attachment in metadata.Items("attachments"))
                messageWriter.WriteAsset(AttachmentWriter.Key(attachment.String("id")), attachment, null);
        }
    }

    /// <summary>메시지 하나의 제목, 본문, 첨부를 쓰고 중복 첨부를 건너뜁니다.</summary>
    private sealed class MessageWriter(TextWriter writer, string role, AttachmentWriter assets, string assetLinkRoot, bool dryRun)
    {
        private readonly HashSet<string> writtenAssets = new(StringComparer.Ordinal);
        private bool hasHeading;

        /// <summary>메시지 작성자 제목을 처음 한 번만 씁니다.</summary>
        private void WriteHeading()
        {
            if (hasHeading)
                return;
            writer.WriteLine(role == "user" ? "## 사용자" : "## ChatGPT");
            writer.WriteLine();
            hasHeading = true;
        }

        /// <summary>비어 있지 않은 메시지 본문을 씁니다.</summary>
        public void WriteText(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return;
            WriteHeading();
            writer.WriteLine(body);
            writer.WriteLine();
        }

        /// <summary>메시지에 아직 쓰지 않은 첨부를 출력합니다.</summary>
        public void WriteAsset(string? key, JsonElement attachment, string? type)
        {
            WriteHeading();
            if (key is null || writtenAssets.Add(key))
                assets.Write(writer, key, attachment, type, assetLinkRoot, dryRun);
        }
    }
}
