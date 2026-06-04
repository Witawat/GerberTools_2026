@echo off
setlocal enabledelayedexpansion

set DOTNET=dotnet
set ROOT=%~dp0

if /I "%1"=="release" ( set CONFIG=Release ) else if /I "%1"=="r" ( set CONFIG=Release ) else if /I "%1"=="--help" ( goto help ) else if /I "%1"=="-h" ( goto help ) else ( set CONFIG=Debug )

echo ========================================
echo   GerberTools Build Script [%CONFIG%]
echo ========================================

REM Check .NET SDK version for net9.0 support
for /f "tokens=1" %%v in ('dotnet --version 2^>nul') do set SDKVER=%%v
set HAS_NET9=0
for /f "tokens=1 delims=." %%a in ("%SDKVER%") do (
    if %%a geq 9 set HAS_NET9=1
)
if %HAS_NET9%==0 (
    echo .NET SDK %SDKVER% detected - net9.0 projects will be SKIPPED
    echo Install .NET 9 SDK for full build
)
echo.

set OUT=%ROOT%Build\Output
set CLI_OUT=%OUT%\CommandLine

REM Clean previous output
if exist "%OUT%" rmdir /s /q "%OUT%"
mkdir "%OUT%" 2>nul
mkdir "%CLI_OUT%" 2>nul

REM ==========================================
echo [%CONFIG%] Building Core Libraries...
call :buildproj GerberLibrary\GerberLibrary.csproj
call :buildproj EagleLoaders\EagleLoaders.csproj
call :buildproj Project_Utilities\TilingLibrary\TINRS-ArtWork.csproj
call :buildproj GerberPanelizer\QuickFont\QuickFont.csproj
if %HAS_NET9%==1 (
    call :buildproj GerberLibrary.Core\GerberLibrary.Core.csproj
    call :buildproj TilingLibrary.Core\TilingLibrary.Core.csproj
)
echo   Core Libraries   [OK]

REM ==========================================
echo [%CONFIG%] Building GUI Applications...
call :buildproj QuickGerberRender\QuickGerberRender.csproj                     & call :copyout QuickGerberRender        QuickGerberRender           net48
call :buildproj GerberPanelizer\GerberPanelize.csproj                          & call :copyout GerberPanelizer          GerberPanelizer             net48
call :buildproj GerberViewer\GerberViewer.csproj                               & call :copyout GerberViewer             GerberViewer                net48
call :buildproj FitBitmapToOutlineAndMerge\FitBitmapToOutlineAndMerge.csproj   & call :copyout FitBitmapToOutlineAndMerge FitBitmapToOutlineAndMerge net48
call :buildproj CaseBuilder\CaseBuilder.csproj                                 & call :copyout CaseBuilder              CaseBuilder                 net48
call :buildproj EagleBoardToCHeader\EagleBoardToCHeader.csproj                 & call :copyout EagleBoardToCHeader      EagleBoardToCHeader         net48
call :buildproj FrontPanelBuilder\FrontPanelBuilder.csproj                    & call :copyout FrontPanelBuilder        FrontPanelBuilder           net48
call :buildproj JLCDrop\JLCDrop.csproj                                         & call :copyout JLCDrop                  JLCDrop                     net48
call :buildproj VScorePanel\FrameDrop.csproj                                   & call :copyout VScorePanel              FrameDrop                   net48
call :buildproj ProductionFrame\ProductionFrame.csproj                         & call :copyout ProductionFrame          ProductionFrame             net48
call :buildproj PnP_Processor\PnP_Processor.csproj                             & call :copyout PnP_Processor            PnP_Processor               net48
call :buildproj SolderTool\SolderTool.csproj                                   & call :copyout SolderTool               SolderTool                  net48
call :buildproj TINRS-ArtWorkGenerator\TINRS-ArtWorkGenerator.csproj           & call :copyout TINRS-ArtWorkGenerator   TINRS-ArtWorkGenerator      net48
call :buildproj Project_Utilities\IconBuilder\IconBuilder.csproj               & call :copyout IconBuilder              IconBuilder                 net48
call :buildproj GerberProjects\OpampCalculator\OpampCalculator.csproj          & call :copyout OpampCalculator          OpampCalculator             net48
if %HAS_NET9%==1 (
    call :buildproj GerberDrop\GerberDrop.csproj                               & call :copyout GerberDrop               GerberDrop                  net9.0
    call :buildproj TiNRS-Tiler\TiNRS.Tiler.csproj                             & call :copyout TiNRS-Tiler              TiNRS.Tiler                 net9.0
) else (
    echo   SKIPPED: GerberDrop (net9.0)
    echo   SKIPPED: TiNRS-Tiler (net9.0)
)
echo   GUI Applications [OK]

REM ==========================================
echo [%CONFIG%] Building CLI Tools...
call :buildproj GerberAnalyse\GerberAnalyse.csproj                     & call :copycli GerberAnalyse            net48
call :buildproj GerberClipper\GerberClipper.csproj                     & call :copycli GerberClipper             net48
call :buildproj GerberCombiner\GerberCombiner.csproj                   & call :copycli GerberCombiner            net48
call :buildproj GerberMover\GerberMover.csproj                         & call :copycli GerberMover               net48
call :buildproj GerberSanitize\GerberSanitize.csproj                   & call :copycli GerberSanitize             net48
call :buildproj GerberSubtract\GerberSubtract.csproj                   & call :copycli GerberSubtract             net48
call :buildproj AutoPanelBuilder\AutoPanelBuilder.csproj               & call :copycli AutoPanelBuilder          net48
call :buildproj AntennaBuilder\NFCAntennaBuilder.csproj                & call :copycli AntennaBuilder            net48
call :buildproj BOMConsolidator\BOMConsolidator.csproj                 & call :copycli BOMConsolidator           net48
call :buildproj ProtoBoardGenerator\ProtoBoardGenerator.csproj         & call :copycli ProtoBoardGenerator       net48
call :buildproj LightPipeBuilder\LightPipeBuilder.csproj               & call :copycli LightPipeBuilder           net48
call :buildproj ImageToGerber\FrontPanelImageToGerber.csproj           & call :copycli ImageToGerber             net48
call :buildproj MakeIcon\MakeIcon.csproj                               & call :copycli MakeIcon                  net48
call :buildproj Project_Utilities\IconScanner\IconScanner.csproj       & call :copycli IconScanner               net48
call :buildproj Project_Utilities\ReleaseBuilder\ReleaseBuilder.csproj & call :copycli ReleaseBuilder            net48
call :buildproj DirtyPCBs\SickOfBeige\DirtyPCB_SickOfBeige.csproj      & call :copycli SickOfBeige               net48
call :buildproj DirtyPCBs\DirtyPCB_BoardStats\DirtyPCB_BoardStats.csproj   & call :copycli BoardStats            net48
call :buildproj DirtyPCBs\DirtyPCB_BoardRender\DirtyPCB_BoardRender.csproj & call :copycli BoardRender           net48
call :buildproj DirtyPCBs\DirtyPCB_DXFStats\DirtyPCB_DXFStats.csproj       & call :copycli DXFStats              net48
call :buildproj DirtyPCBs\Base64Extractor\DirtyPCB_Base64Extractor.csproj  & call :copycli Base64Extractor       net48
call :buildproj DirtyPCBs\DirtyLocaleTest\DirtyPCB_LocaleTest.csproj       & call :copycli LocaleTest            net48
call :buildproj GerberProjects\EagleLoadTest\EagleLoadTest.csproj          & call :copycli EagleLoadTest         net48
call :buildproj GerberProjects\FrameCreatorTest\FrameCreatorTest.csproj    & call :copycli FrameCreatorTest      net48
if %HAS_NET9%==1 (
    call :buildproj GerberDebugger\GerberDebugger.csproj               & call :copycli GerberDebugger            net9.0
    call :buildproj GerberSplitter\GerberSplitter.csproj               & call :copycli GerberSplitter             net9.0
    call :buildproj GerberToDxf\GerberToDxf\GerberToDxf.csproj         & call :copycli GerberToDxf               net9.0
    call :buildproj GerberToImage\GerberToImage.csproj                 & call :copycli GerberToImage             net9.0
    call :buildproj GerberToOutline\GerberToOutline.csproj             & call :copycli GerberToOutline           net9.0
    call :buildproj Tests\GerberTools.TestGenerator\GerberTools.TestGenerator.csproj & call :copycli TestGenerator   net9.0
    call :buildproj MigrationTest\MigrationTest.csproj                 & call :copycli MigrationTest             net9.0
) else (
    echo   SKIPPED: net9.0 CLI tools ^(GerberDebugger, GerberSplitter, GerberToDxf, GerberToImage, GerberToOutline, TestGenerator, MigrationTest^)
)
echo   CLI Tools        [OK]

REM ==========================================
echo ========================================
echo   All builds complete!
echo ========================================
echo.
echo Output: %OUT%
echo.
echo --- GUI Applications ---
dir /b /ad "%OUT%" 2>nul | findstr /V "CommandLine"
echo.
echo --- CLI Tools (%CLI_OUT%) ---
dir /b "%CLI_OUT%" 2>nul | findstr "."
echo.
goto end

REM ==========================================
REM  Helper: Build a project
REM ==========================================
:buildproj
echo   Building: %~1
"%DOTNET%" build "%ROOT%%~1" -c %CONFIG% --nologo -v q >nul 2>&1
if not %ERRORLEVEL%==0 (
    echo   FAILED: %~1
    "%DOTNET%" build "%ROOT%%~1" -c %CONFIG% --nologo 2>&1 | findstr /C:"error"
    exit /b %ERRORLEVEL%
)
exit /b 0

REM ==========================================
REM  Helper: Copy GUI app output
REM  %1 = source folder, %2 = dest name, %3 = framework (net48/net9.0)
REM ==========================================
:copyout
set SRCDIR=%ROOT%%~1\bin\%CONFIG%\%~3\
set DSTDIR=%OUT%\%~2\
if exist "%DSTDIR%" rmdir /s /q "%DSTDIR%"
mkdir "%DSTDIR%" 2>nul
xcopy "%SRCDIR%*" "%DSTDIR%" /Y /Q /E >nul 2>&1
echo     -> %OUT:|=:%\%~2
exit /b 0

REM ==========================================
REM  Helper: Copy CLI tool output
REM  %1 = source folder, %2 = framework (net48/net9.0)
REM ==========================================
:copycli
set SRCDIR=%ROOT%%~1\bin\%CONFIG%\%~2\
xcopy "%SRCDIR%*" "%CLI_OUT%\" /Y /Q >nul 2>&1
echo     -> %OUT:|=:%\CommandLine\
exit /b 0

:fail
echo ========================================
echo   BUILD FAILED
echo ========================================
exit /b %ERRORLEVEL%

:help
echo Usage: build_all.bat [debug^|release]
echo.
echo   (no arg)  Build Debug, copy output to Build\Output\
echo   debug/d   Build Debug
echo   release/r Build Release
echo.
echo Output:
echo   Build\Output\[AppName]\  -- GUI applications
echo   Build\Output\CommandLine\ -- CLI tools
goto end

:end
endlocal
