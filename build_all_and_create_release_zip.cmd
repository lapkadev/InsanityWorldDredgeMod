@echo off
dotnet run --project "%~dp0Tools" -- release-zip
if errorlevel 1 pause
