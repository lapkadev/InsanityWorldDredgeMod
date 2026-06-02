@echo off
dotnet run --project "%~dp0Tools" -- bump-patch
if errorlevel 1 pause
