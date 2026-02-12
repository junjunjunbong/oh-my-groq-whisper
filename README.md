# Groq Whisper Push-to-Talk

Windows용 Groq Whisper STT 기반 푸시투토크 음성 인식 앱입니다.

## 기능

- **Ctrl+Shift+Space**를 누르는 동안 녹음
- 키를 떼면 Groq Whisper API로 자동 전사
- 커서 근처에 오버레이로 결과 표시
- 텍스트 편집 및 클립보드 복사 지원
- ESC 키 또는 X 버튼으로 오버레이 닫기
- **오버레이 창 크기 조절** (오른쪽 하단 드래그)
- **창 크기에 따라 글자 크기 자동 조절**
- **전사 중에도 핫키로 바로 새 녹음 시작** (창 닫기 불필요)

## 요구사항

- Windows 10/11
- .NET 8.0
- Groq API Key
- 마이크

## 설치 및 실행

### 1. API Key 설정

PowerShell:
```powershell
$env:GROQ_API_KEY = "your-groq-api-key"
```

CMD:
```cmd
set GROQ_API_KEY=your-groq-api-key
```

시스템 환경변수로 영구 설정:
```powershell
[Environment]::SetEnvironmentVariable("GROQ_API_KEY", "your-groq-api-key", "User")
```

### 2. 빌드 및 실행

```bash
# 빌드
dotnet build

# 실행
dotnet run
```

### 3. 단일 EXE로 게시

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

출력 위치: `bin/Release/net8.0-windows/win-x64/publish/GroqWhisperPTT.exe`

## 사용법

1. 앱 실행 (트레이에 아이콘 없이 백그라운드 실행)
2. **Ctrl+Shift+Space**를 누르고 있으면 녹음 시작
3. 키를 떼면 녹음 종료 및 전사 요청
4. 오버레이에 전사 결과가 표시됨
5. 텍스트를 편집하거나 **Copy** 버튼으로 클립보드에 복사
6. **Esc** 키 또는 **X 버튼**으로 오버레이 닫기
7. **오버레이 창 크기**를 오른쪽 하단에서 조절 가능 (글자 크기도 함께 변경)
8. 전사 결과 화면에서도 **Ctrl+Shift+Space**로 바로 새 녹음 시작 가능

## 프로젝트 구조

```
/src
  /Core
    AppStateMachine.cs    # 상태 관리 (Idle, Recording, Finalizing, Editing, Error)
    Models.cs             # 상태 및 이벤트 DTO
  /Input
    HotkeyHook.cs         # 글로벌 키보드 훅 (Ctrl+Shift+Space)
  /Audio
    AudioRecorder.cs      # NAudio 기반 녹음
  /STT
    GroqTranscriptionService.cs  # Groq Whisper API 호출
    GroqModels.cs                # API 응답 모델
  /UI
    OverlayWindow.xaml(.cs)  # 오버레이 UI (크기 조절, 글자 크기 조절 지원)
  /Util
    ClipboardService.cs   # 클립보드 복사
    TempFileCleaner.cs    # 임시 파일 정리
    SettingsManager.cs    # 창 크기 등 설정 저장/로드
```

## 기술 스택

- .NET 8 WPF
- NAudio (오디오 캡처)
- Groq Whisper API (STT)
- System.Text.Json (설정 파일)

## 라이선스

MIT
