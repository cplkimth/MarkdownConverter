rg --no-filename --no-line-number --only-matching --replace '$1' '"conversation_template_id"\s*:\s*"(g-p-[^"]+)"' download -g 'conversations*.json' |
    Sort-Object -Unique