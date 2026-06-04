@echo off
setlocal
set DOTNET=C:\Program Files\dotnet\dotnet.exe
set PROJ=%~dp0QuickGerberRender\QuickGerberRender.csproj

if /I "%1"=="release" goto release
if /I "%1"=="r" goto release
if /I "%1"=="debug" goto debug
if /I "%1"=="d" goto debug
if /I "%1"=="--help" goto help
if /I "%1"=="-h" goto help

echo ========================================
echo   QuickGerberRender Build Script
echo ========================================
echo Usage: %~nx0 [debug^|release]
echo.
echo   No args : Build Debug
echo   debug/d : Build Debug
echo   release/r: Build Release
echo.
echo Current: Building DEBUG...
echo ========================================
goto debug

:debug
echo.
echo [Debug Build]
"%DOTNET%" build "%PROJ%" -c Debug
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%
echo.
echo Debug output: %~dp0QuickGerberRender\bin\Debug\net48\
goto end

:release
echo.
echo [Release Build]
"%DOTNET%" build "%PROJ%" -c Release
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%
echo.
echo Release output: %~dp0QuickGerberRender\bin\Release\net48\
goto end

:help
echo Usage: %~nx0 [debug^|release]
echo.
echo   (no arg)  Build Debug
echo   debug/d   Build Debug
echo   release/r Build Release
goto end

:end
endlocal
