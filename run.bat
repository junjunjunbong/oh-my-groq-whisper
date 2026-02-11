@echo off
chcp 65001 >nul

if exist "%~dp0.env" (
    for /f "usebackq tokens=1,* delims==" %%A in ("%~dp0.env") do (
        set "%%A=%%B"
    )
)

if "%GROQ_API_KEY%"=="" (
    echo GROQ_API_KEY가 설정되지 않았습니다.
    echo.
    echo .env 파일을 만들어주세요:
    echo   copy .env.sample .env
    echo   메모장으로 .env 열어서 API 키 입력
    echo.
    pause
    exit /b 1
)

dotnet run --project "%~dp0."
pause
