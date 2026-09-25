@echo off
title PommeBar - Dependency Installer
echo ===================================================
echo   PommeBar: Restoring .NET NuGet Dependencies
echo ===================================================
echo.

where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] .NET 8 SDK is not found in your PATH.
    echo Please download and install .NET 8 SDK from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo Running: dotnet restore ...
dotnet restore
if %ERRORLEVEL% EQU 0 (
    echo.
    echo [SUCCESS] All dependencies restored successfully!
    echo You can now build with 'dotnet build' or run with 'dotnet run'.
) else (
    echo.
    echo [ERROR] dotnet restore failed. Check internet connection or NuGet configuration.
)

echo.
pause
