@echo off
echo ========================================
echo   Building Smooth Scroll GUI App
echo ========================================
echo.

REM Change to the directory where this batch file is located
cd /d "%~dp0"

REM Check if dotnet is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 6.0 SDK or later from:
    echo https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Check if project file exists
if not exist "SmoothScrollApp.csproj" (
    echo ERROR: SmoothScrollApp.csproj not found!
    echo Make sure all files are in the same directory.
    echo.
    echo Current directory: %CD%
    pause
    exit /b 1
)

echo Cleaning previous build...
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"

echo Restoring packages...
dotnet restore SmoothScrollApp.csproj

echo Building Release version...
dotnet build SmoothScrollApp.csproj -c Release --no-restore

if errorlevel 1 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

REM Copy exe to root folder for convenience
echo.
echo Copying executable to root folder...
if exist "bin\Release\net6.0-windows\SmoothScrollApp.exe" (
    copy /Y "bin\Release\net6.0-windows\SmoothScrollApp.exe" "SmoothScrollApp.exe" >nul
    copy /Y "bin\Release\net6.0-windows\*.dll" "." >nul
    copy /Y "bin\Release\net6.0-windows\*.json" "." >nul
    echo Files copied successfully!
) else (
    echo WARNING: Could not find built files. Check bin\Release\net6.0-windows\ folder manually.
)

echo.
echo ========================================
echo   Build Complete!
echo ========================================
echo.
echo Executable location:
echo SmoothScrollApp.exe (in the current folder)
echo.
echo To run the application:
echo 1. Right-click SmoothScrollApp.exe
echo 2. Select "Run as administrator"
echo.
echo The app will run in the system tray!
echo.
pause