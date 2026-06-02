@echo off
dotnet run --project "%~dp0Tools" -- archive
if errorlevel 1 pause
