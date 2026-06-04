@echo off
setlocal
set DOTNET=C:\Program Files\dotnet\dotnet.exe
set ROOT=%~dp0

if /I "%1"=="release" goto release
if /I "%1"=="r" goto release
if /I "%1"=="debug" goto debug
if /I "%1"=="d" goto debug
if /I "%1"=="--help" goto help
if /I "%1"=="-h" goto help

echo ========================================
echo   GerberTools Build Script
echo ========================================
echo Usage: %~nx0 [debug^|release]
echo.
echo   (no arg)  Build Debug
echo   debug/d   Build Debug
echo   release/r Build Release
echo.
echo Current: Building DEBUG...
echo ========================================
goto debug

:debug
set CONFIG=Debug
goto build

:release
set CONFIG=Release
goto build

:build
echo.
echo [%CONFIG%] Building GerberLibrary...
"%DOTNET%" build "%ROOT%GerberLibrary\GerberLibrary.csproj" -c %CONFIG%
if %ERRORLEVEL% neq 0 goto fail

echo.
echo [%CONFIG%] Building QuickGerberRender...
"%DOTNET%" build "%ROOT%QuickGerberRender\QuickGerberRender.csproj" -c %CONFIG%
if %ERRORLEVEL% neq 0 goto fail

echo.
echo [%CONFIG%] Building GerberPanelizer...
"%DOTNET%" build "%ROOT%GerberPanelizer\GerberPanelize.csproj" -c %CONFIG%
if %ERRORLEVEL% neq 0 goto fail

echo.
echo [%CONFIG%] Building GerberViewer...
"%DOTNET%" build "%ROOT%GerberViewer\GerberViewer.csproj" -c %CONFIG%
if %ERRORLEVEL% neq 0 goto fail

echo.
echo ========================================
echo   Collecting output files...
echo ========================================

set OUT=%ROOT%Build\Output
if exist "%OUT%" rmdir /s /q "%OUT%"

:: QuickGerberRender
set SRC=%ROOT%QuickGerberRender\bin\%CONFIG%\net48\
set DST=%OUT%\QuickGerberRender\
mkdir "%DST%" 2>nul
xcopy "%SRC%*" "%DST%" /Y /Q /E >nul
echo   QuickGerberRender  [OK]

:: GerberPanelizer
set SRC=%ROOT%GerberPanelizer\bin\%CONFIG%\net48\
set DST=%OUT%\GerberPanelizer\
mkdir "%DST%" 2>nul
xcopy "%SRC%*" "%DST%" /Y /Q /E >nul
echo   GerberPanelizer    [OK]

:: GerberViewer
set SRC=%ROOT%GerberViewer\bin\%CONFIG%\net48\
set DST=%OUT%\GerberViewer\
mkdir "%DST%" 2>nul
xcopy "%SRC%*" "%DST%" /Y /Q /E >nul
echo   GerberViewer       [OK]

echo.
echo ========================================
echo   All builds succeeded!
echo ========================================
echo.
echo Output: %OUT%
echo.
echo   QuickGerberRender\QuickGerberRender.exe
echo   GerberPanelizer\GerberPanelizer.exe
echo   GerberViewer\GerberViewer.exe
echo.
goto end

:fail
echo.
echo ========================================
echo   BUILD FAILED
echo ========================================
exit /b %ERRORLEVEL%

:help
echo Usage: %~nx0 [debug^|release]
echo.
echo   (no arg)  Build Debug
echo   debug/d   Build Debug
echo   release/r Build Release
echo.
echo Output: Build\Output\[AppName]\
goto end

:end
endlocal
