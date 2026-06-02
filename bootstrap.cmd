@echo off
REM Fetches vanilla DREDGE DLLs into ModUnity/Assets/Plugins/Dredge/ (via NuGet).
REM Run once on a fresh clone before opening Unity. Re-run if DREDGE/Winch versions change.

dotnet run --project "%~dp0Tools\Bootstrap"
if errorlevel 1 pause
