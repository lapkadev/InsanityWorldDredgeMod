@echo off
dotnet run --project "%~dp0Tools" -- deploy
if errorlevel 1 pause
