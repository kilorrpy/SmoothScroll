# Smooth Scroll

Full free System-wide smooth scrolling for Windows with customizable settings and dark theme.

## Features

- System-wide smooth scrolling
- Customizable smoothness, speed, and update rate
- Light and dark themes
- Runs in system tray
- Lightweight and minimal CPU usage

## Installation

**Requirements:** Windows 10/11 and [.NET 6.0 SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/kilorrpy/SmoothScroll.git
cd smoothscroll
build_app.bat
```

Run `SmoothScrollApp.exe` as administrator.

## Usage

- **System tray icon** - Right-click for menu
- **Settings** - Double-click tray icon
- **Theme toggle** - Button in top-right corner
- **Enable/Disable** - Right-click menu option

Settings are saved automatically to `%APPDATA%\SmoothScroll\settings.json`

## Building

```bash
dotnet build SmoothScrollApp.csproj -c Release
```

Or run `build_app.bat`

## Support

[Buy me a coffee ☕](https://www.donationalerts.com/r/kilorrpy)

## License

MIT License - see [LICENSE](LICENSE) file
