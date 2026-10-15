@echo off
cd /d "%~dp0"
dotnet build 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED with exit code %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)
echo.
echo BUILD SUCCEEDED
