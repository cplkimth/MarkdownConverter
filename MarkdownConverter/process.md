# MarkdownConverter 실행 흐름

프로그램은 내보낸 대화 JSON을 두 번 읽습니다. 첫 번째 읽기에서 대화별 출력 경로를 확정하고, 두 번째 읽기에서 기존 파일을 이전하거나 새 Markdown을 기록합니다.

```mermaid
flowchart TD
    Start["Program.Main<br/>UTF-8 콘솔 설정 및 전달된 인수 출력"] --> Help{"--help 지정?"}
    Help -- 예 --> HelpExit["사용법 출력 · 종료 코드 0"]
    Help -- 아니요 --> Options["ConversionOptions<br/>인수 해석 및 절대 경로 확정"]
    Options --> Guard["MarkdownFiles<br/>입출력 경로의 포함 관계와 출력 폴더 링크 검사"]
    Guard --> Input["ConversationExport<br/>분할 JSON을 이름순으로 찾거나 단일 JSON 선택"]
    Input --> Plan["첫 번째 읽기 · OutputPlanner<br/>대화 ID 검증, 프로젝트별 분류, 파일명 충돌 해결"]
    Plan --> Prepare["예정된 폴더 검사 · 이전 형식 파일과 첨부 목록 조회"]
    Prepare --> Next{"두 번째 읽기<br/>처리할 대화가 있는가?"}
    Next -- 예 --> Owner{"대상 경로가 다른 대화의 파일인가?"}
    Owner -- 예 --> Failed["해당 대화 오류 기록 및 실패 수 집계"]
    Owner -- 아니요 --> Legacy{"이전 형식 파일이 있는가?"}
    Legacy -- 예 --> Move["기존 파일 이전 · dry-run에서는 이전 예정 수만 집계"]
    Legacy -- 아니요 --> Exists{"대상 파일이 이미 있는가?"}
    Exists -- 예 --> Skip["기존 파일 집계"]
    Exists -- 아니요 --> Mode{"--dry-run 지정?"}
    Mode -- 예 --> Simulate["MarkdownRenderer<br/>텍스트는 버리고 첨부 참조를 확인"]
    Mode -- 아니요 --> Render["MarkdownRenderer<br/>현재 분기의 표시 가능한 메시지를 Markdown으로 변환"]
    Render --> Assets["AttachmentWriter<br/>이미지는 본문에 포함하고 다른 첨부는 복사한 뒤 링크 기록"]
    Assets --> Commit["MarkdownFiles<br/>임시 파일을 최종 경로로 이동"]
    Move --> Next
    Skip --> Next
    Simulate --> Next
    Commit --> Next
    Failed --> Next
    Next -- 아니요 --> Summary["출력 경로와 처리 결과 출력"]
    Summary --> Exit{"실패 또는 누락 첨부가 있는가?"}
    Exit -- 예 --> ErrorExit["종료 코드 1"]
    Exit -- 아니요 --> Success["종료 코드 0"]
```

- 기본 출력 경로는 실행 디렉터리 아래의 서울 시각 `yyyyMMdd-HHmm` 폴더입니다. `--output`을 지정하면 해당 경로를 사용합니다.
- 출력 파일명은 `제목_yyyyMMdd_HHmmss.md` 형식입니다. 같은 이름이 겹치면 대화 ID 순서에 따라 `_2`부터 번호를 붙입니다.
- 대화 처리 중 프로그램이 처리할 수 있는 예외는 해당 대화의 실패로 집계하고 다음 대화를 처리합니다. 준비 단계에서 처리할 수 있는 오류가 나면 오류를 표시하고 종료 코드 1을 반환합니다.
