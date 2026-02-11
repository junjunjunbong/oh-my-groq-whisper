# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GroqWhisperPTT — Windows 전용 Push-to-Talk 음성 인식 데스크톱 앱. Ctrl+Shift+Space 글로벌 단축키로 음성을 녹음하고, Groq Whisper API(whisper-large-v3-turbo)로 실시간 텍스트 변환 후 오버레이 창에 결과를 표시한다.

## Build & Run Commands

```bash
# 빌드
dotnet build

# 실행 (GROQ_API_KEY 환경변수 필수)
dotnet run

# 릴리즈 배포용 단일 EXE 빌드
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

테스트 프레임워크 없음. 수동 테스트만 지원.

## Architecture

이벤트 기반 상태 머신(FSM) 패턴. `App.xaml.cs`가 오케스트레이터로 모든 서비스를 초기화하고 연결한다.

**상태 흐름:**
```
Idle → (HotkeyDown) → Recording → (HotkeyUp) → Finalizing → (Success) → Editing → (Copy/Cancel) → Idle
                                                            → (Fail) → Error → (Cancel/Retry) → Idle
```

**레이어 구조:**

| 레이어 | 경로 | 역할 |
|--------|------|------|
| Core | `Core/AppStateMachine.cs`, `Core/Models.cs` | FSM 로직, AppState/AppEvent enum |
| Input | `Input/HotkeyHook.cs` | SetWindowsHookEx P/Invoke 글로벌 키보드 훅 |
| Audio | `Audio/AudioRecorder.cs` | NAudio WaveInEvent, 16kHz/16bit/Mono WAV 녹음 |
| STT | `STT/GroqTranscriptionService.cs`, `STT/GroqModels.cs` | Groq API 호출 (multipart/form-data), 응답 파싱 |
| UI | `UI/OverlayWindow.xaml(.cs)` | WPF 오버레이 (녹음/변환중/편집/에러 상태별 표시) |
| Util | `Util/ClipboardService.cs`, `Util/TempFileCleaner.cs` | 클립보드, 임시 WAV 정리 |

**진입점:** `App.xaml.cs` — 환경변수 체크 → 서비스 초기화 → 이벤트 핸들러 등록 → 키보드 훅 시작

## Key Technical Details

- **Target:** .NET 8.0, WinExe, WPF (`net8.0-windows`)
- **Nullable:** enabled (strict null-safety)
- **유일한 NuGet 패키지:** NAudio 2.2.1 (오디오 캡처)
- **환경변수:** `GROQ_API_KEY` 필수 — 미설정 시 앱 시작 불가
- **API 엔드포인트:** `https://api.groq.com/openai/v1/audio/transcriptions`
- **최소 녹음 시간:** 300ms (미만 시 전송하지 않음)
- **API 타임아웃:** 60초
- **오버레이:** 커서 근처 배치, TopMost, WindowStyle=None, DPI 인식

## Code Conventions

- 파일 스코프 네임스페이스 (C# 11)
- PascalCase (public 멤버), `_camelCase` (private 필드)
- async/await 패턴, CancellationToken 지원
- P/Invoke는 `Input/HotkeyHook.cs`에 집중
- 상태 머신은 순수 로직만 (side-effect 없음), 부수 효과는 `App.xaml.cs`에서 처리
