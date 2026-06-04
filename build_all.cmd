@echo off
dotnet run --project "%~dp0Tools" -- build-all
if errorlevel 1 pause
