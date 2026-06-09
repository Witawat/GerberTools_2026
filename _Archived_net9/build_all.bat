@echo off
echo ============================================
echo  Building all _Archived_net9 projects
echo ============================================
echo.

set PASS=0
set FAIL=0

echo [1/11] GerberLibrary.Core (library)
dotnet build GerberLibrary.Core\GerberLibrary.Core.csproj -c Release -v q
if %ERRORLEVEL% EQU 0 (echo   PASS && set /a PASS+=1) else (echo   FAIL && set /a FAIL+=1)

echo [2/11] TilingLibrary.Core (library)
dotnet build TilingLibrary.Core\TilingLibrary.Core.csproj -c Release -v q
if %ERRORLEVEL% EQU 0 (echo   PASS && set /a PASS+=1) else (echo   FAIL && set /a FAIL+=1)

echo [3/11] GerberDebugger (CLI)
dotnet build GerberDebugger\GerberDebugger.csproj -c Release -v q
if %ERRORLEVEL% EQU 0 (echo   PASS && set /a PASS+=1) else (echo   FAIL && set /a FAIL+=1)

echo [4/11] GerberSplitter (CLI)
dotnet build GerberSplitter\GerberSplitter.csproj -c Release -v q
if %ERRORLEVEL% EQU 0 (echo   PASS && set /a PASS+=1) else (echo   FAIL && set /a FAIL+=1)

echo [5/11] GerberToDxf (CLI)
dotnet build GerberToDxf\GerberToDxf.csproj -c Release -v q
if %ERRORLEVEL% EQU 0 (echo   PASS && set /a PASS+=1) else (echo   FAIL && set /a FAIL+=1)

echo [6/11] GerberToImage (CLI)
dotnet build GerberToImage\GerberToImage.csproj -c Release -v q
if %ERRORLEVEL% EQU 0 (echo   PASS && set /a PASS+=1) else (echo   FAIL && set /a FAIL+=1)

echo [7/11] GerberToOutline (CLI)
dotnet build GerberToOutline\GerberToOutline.csproj -c Release -v q
if %ERRORLEVEL% EQU 0 (echo   PASS && set /a PASS+=1) else (echo   FAIL && set /a FAIL+=1)

echo [8/11] GerberTools.TestGenerator (CLI)
dotnet build GerberTools.TestGenerator\GerberTools.TestGenerator.csproj -c Release -v q
if %ERRORLEVEL% EQU 0 (echo   PASS && set /a PASS+=1) else (echo   FAIL && set /a FAIL+=1)

echo [9/11] MigrationTest (CLI)
dotnet build MigrationTest\MigrationTest.csproj -c Release -v q
if %ERRORLEVEL% EQU 0 (echo   PASS && set /a PASS+=1) else (echo   FAIL && set /a FAIL+=1)

echo [10/11] GerberDrop (GUI - Avalonia)
dotnet build GerberDrop\GerberDrop.csproj -c Release -v q
if %ERRORLEVEL% EQU 0 (echo   PASS && set /a PASS+=1) else (echo   FAIL && set /a FAIL+=1)

echo [11/11] TiNRS-Tiler (GUI - Avalonia)
dotnet build TiNRS-Tiler\TiNRS.Tiler.csproj -c Release -v q
if %ERRORLEVEL% EQU 0 (echo   PASS && set /a PASS+=1) else (echo   FAIL && set /a FAIL+=1)

echo.
echo ============================================
echo  Build Summary: %PASS% PASS / %FAIL% FAIL
echo ============================================
pause
