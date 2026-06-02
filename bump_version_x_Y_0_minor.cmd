@echo off
dotnet run --project "%~dp0Tools" -- bump-minor
if errorlevel 1 pause
