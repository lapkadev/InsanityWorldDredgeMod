@echo off
dotnet run --project "%~dp0Tools" -- bump-major
if errorlevel 1 pause
