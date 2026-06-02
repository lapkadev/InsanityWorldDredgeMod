@echo off
dotnet run --project "%~dp0Tools" -- bootstrap
if errorlevel 1 pause
