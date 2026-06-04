@echo off
setlocal enabledelayedexpansion
title GerberTools Build

set DOTNET=dotnet
set ROOT=%~dp0
set SLN=%ROOT%GerberTools.sln
set OUT=%ROOT%Build\Output
set CLI_OUT=%OUT%\CommandLine

REM Check .NET SDK
for /f "tokens=1" %%v in ('dotnet --version 2^>nul') do set SDKVER=%%v
set HAS_NET9=0
for /f "tokens=1 delims=." %%a in ("%SDKVER%") do if %%a geq 9 set HAS_NET9=1

if "%1"=="--help" goto help
if "%1"=="-h" goto help

REM =============================================
REM  Interactive menu
REM =============================================
:menu
cls
echo.
echo  ========================================
echo    GerberTools Build Script
echo  ========================================
echo    .NET SDK: %SDKVER%
if %HAS_NET9%==0 echo    (net9.0 projects will be skipped)
echo.
echo    [1] Build ALL (Debug)
echo    [2] Build ALL (Release)
echo    [3] Build single project
echo    [Q] Quit
echo  ========================================
echo.
set /p CHOICE="  Enter choice [1-3/Q]: "

if /I "%CHOICE%"=="Q" goto end
if "%CHOICE%"=="1" (set CONFIG=Debug& goto build)
if "%CHOICE%"=="2" (set CONFIG=Release& goto build)
if "%CHOICE%"=="3" (set CONFIG=Debug& goto single)
echo  Invalid choice. Try again.
timeout /t 1 >nul
goto menu

REM =============================================
REM  Build via solution (fast, parallel)
REM =============================================
:build
cls
echo.
echo  ========================================
echo    Building GerberTools.sln [%CONFIG%]
echo  ========================================
echo.
"%DOTNET%" build "%SLN%" -c %CONFIG% --nologo
if not %ERRORLEVEL%==0 (
    echo.
    echo  ========================================
    echo    BUILD FAILED
    echo  ========================================
    pause
    goto menu
)

REM Copy outputs
echo.
echo  ========================================
echo    Copying outputs to Build\Output\ ...
echo  ========================================
echo.
if exist "%OUT%" rmdir /s /q "%OUT%"
mkdir "%OUT%" 2>nul
mkdir "%CLI_OUT%" 2>nul

REM Copy GUI apps (each to own folder)
call :copygui QuickGerberRender        QuickGerberRender           net48
call :copygui GerberPanelizer          GerberPanelizer             net48
call :copygui GerberViewer             GerberViewer                net48
call :copygui FitBitmapToOutlineAndMerge FitBitmapToOutlineAndMerge net48
call :copygui CaseBuilder              CaseBuilder                 net48
call :copygui EagleBoardToCHeader      EagleBoardToCHeader         net48
call :copygui FrontPanelBuilder        FrontPanelBuilder           net48
call :copygui JLCDrop                  JLCDrop                     net48
call :copygui VScorePanel              FrameDrop                   net48
call :copygui ProductionFrame          ProductionFrame             net48
call :copygui PnP_Processor            PnP_Processor               net48
call :copygui SolderTool               SolderTool                  net48
call :copygui TINRS-ArtWorkGenerator   TINRS-ArtWorkGenerator      net48
call :copygui Project_Utilities\IconBuilder IconBuilder            net48
call :copygui GerberProjects\OpampCalculator OpampCalculator       net48

REM Copy CLI to CommandLine
call :copycli GerberAnalyse            net48
call :copycli GerberClipper            net48
call :copycli GerberCombiner           net48
call :copycli GerberMover              net48
call :copycli GerberSanitize            net48
call :copycli GerberSubtract           net48
call :copycli AutoPanelBuilder         net48
call :copycli AntennaBuilder           net48
call :copycli BOMConsolidator          net48
call :copycli ProtoBoardGenerator      net48
call :copycli LightPipeBuilder          net48
call :copycli ImageToGerber            net48
call :copycli MakeIcon                 net48
call :copycli Project_Utilities\IconScanner  net48
call :copycli Project_Utilities\ReleaseBuilder net48
call :copycli DirtyPCBs\SickOfBeige     net48
call :copycli DirtyPCBs\DirtyPCB_BoardStats net48
call :copycli DirtyPCBs\DirtyPCB_BoardRender net48
call :copycli DirtyPCBs\DirtyPCB_DXFStats   net48
call :copycli DirtyPCBs\Base64Extractor     net48
call :copycli DirtyPCBs\DirtyLocaleTest     net48
call :copycli GerberProjects\EagleLoadTest  net48
call :copycli GerberProjects\FrameCreatorTest net48

echo.
echo  ========================================
echo    Build complete  [%CONFIG%]
echo  ========================================
echo    Output: %OUT%
echo.
if exist "%OUT%" (
    for /d %%d in ("%OUT%\*") do if /I not "%%~nxd"=="CommandLine" echo    %%~nxd
)
echo.
echo    CLI tools: %CLI_OUT%
echo.
pause
goto menu

REM =============================================
REM  Build single project
REM =============================================
:single
cls
echo.
echo  ========================================
echo    Build Single Project
echo  ========================================
echo.
echo  --- GUI Applications ---
echo    [01] GerberPanelizer          [02] GerberViewer
echo    [03] QuickGerberRender        [04] FitBitmapToOutlineAndMerge
echo    [05] CaseBuilder              [06] EagleBoardToCHeader
echo    [07] FrontPanelBuilder        [08] JLCDrop
echo    [09] VScorePanel              [10] ProductionFrame
echo    [11] PnP_Processor            [12] SolderTool
echo    [13] TINRS-ArtWorkGenerator   [14] IconBuilder
echo    [15] OpampCalculator
echo.
echo  --- CLI Tools ---
echo    [16] GerberAnalyse            [17] GerberClipper
echo    [18] GerberCombiner           [19] GerberMover
echo    [20] GerberSanitize           [21] GerberSubtract
echo    [22] AutoPanelBuilder         [23] AntennaBuilder
echo    [24] BOMConsolidator          [25] ProtoBoardGenerator
echo    [26] LightPipeBuilder         [27] ImageToGerber
echo    [28] MakeIcon                 [29] IconScanner
echo    [30] ReleaseBuilder           [31] SickOfBeige
echo    [32] BoardStats               [33] BoardRender
echo    [34] DXFStats                 [35] Base64Extractor
echo    [36] LocaleTest               [37] EagleLoadTest
echo    [38] FrameCreatorTest
echo.
echo    [0] Back to main menu
echo  ========================================
echo.
set /p PICK="  Enter number [0-38]: "

if "%PICK%"=="0" goto menu
if "%PICK%"=="1"  set PROJPATH=GerberPanelizer\GerberPanelize.csproj& goto dosingle
if "%PICK%"=="2"  set PROJPATH=GerberViewer\GerberViewer.csproj& goto dosingle
if "%PICK%"=="3"  set PROJPATH=QuickGerberRender\QuickGerberRender.csproj& goto dosingle
if "%PICK%"=="4"  set PROJPATH=FitBitmapToOutlineAndMerge\FitBitmapToOutlineAndMerge.csproj& goto dosingle
if "%PICK%"=="5"  set PROJPATH=CaseBuilder\CaseBuilder.csproj& goto dosingle
if "%PICK%"=="6"  set PROJPATH=EagleBoardToCHeader\EagleBoardToCHeader.csproj& goto dosingle
if "%PICK%"=="7"  set PROJPATH=FrontPanelBuilder\FrontPanelBuilder.csproj& goto dosingle
if "%PICK%"=="8"  set PROJPATH=JLCDrop\JLCDrop.csproj& goto dosingle
if "%PICK%"=="9"  set PROJPATH=VScorePanel\FrameDrop.csproj& goto dosingle
if "%PICK%"=="10" set PROJPATH=ProductionFrame\ProductionFrame.csproj& goto dosingle
if "%PICK%"=="11" set PROJPATH=PnP_Processor\PnP_Processor.csproj& goto dosingle
if "%PICK%"=="12" set PROJPATH=SolderTool\SolderTool.csproj& goto dosingle
if "%PICK%"=="13" set PROJPATH=TINRS-ArtWorkGenerator\TINRS-ArtWorkGenerator.csproj& goto dosingle
if "%PICK%"=="14" set PROJPATH=Project_Utilities\IconBuilder\IconBuilder.csproj& goto dosingle
if "%PICK%"=="15" set PROJPATH=GerberProjects\OpampCalculator\OpampCalculator.csproj& goto dosingle
if "%PICK%"=="16" set PROJPATH=GerberAnalyse\GerberAnalyse.csproj& goto dosingle
if "%PICK%"=="17" set PROJPATH=GerberClipper\GerberClipper.csproj& goto dosingle
if "%PICK%"=="18" set PROJPATH=GerberCombiner\GerberCombiner.csproj& goto dosingle
if "%PICK%"=="19" set PROJPATH=GerberMover\GerberMover.csproj& goto dosingle
if "%PICK%"=="20" set PROJPATH=GerberSanitize\GerberSanitize.csproj& goto dosingle
if "%PICK%"=="21" set PROJPATH=GerberSubtract\GerberSubtract.csproj& goto dosingle
if "%PICK%"=="22" set PROJPATH=AutoPanelBuilder\AutoPanelBuilder.csproj& goto dosingle
if "%PICK%"=="23" set PROJPATH=AntennaBuilder\NFCAntennaBuilder.csproj& goto dosingle
if "%PICK%"=="24" set PROJPATH=BOMConsolidator\BOMConsolidator.csproj& goto dosingle
if "%PICK%"=="25" set PROJPATH=ProtoBoardGenerator\ProtoBoardGenerator.csproj& goto dosingle
if "%PICK%"=="26" set PROJPATH=LightPipeBuilder\LightPipeBuilder.csproj& goto dosingle
if "%PICK%"=="27" set PROJPATH=ImageToGerber\FrontPanelImageToGerber.csproj& goto dosingle
if "%PICK%"=="28" set PROJPATH=MakeIcon\MakeIcon.csproj& goto dosingle
if "%PICK%"=="29" set PROJPATH=Project_Utilities\IconScanner\IconScanner.csproj& goto dosingle
if "%PICK%"=="30" set PROJPATH=Project_Utilities\ReleaseBuilder\ReleaseBuilder.csproj& goto dosingle
if "%PICK%"=="31" set PROJPATH=DirtyPCBs\SickOfBeige\DirtyPCB_SickOfBeige.csproj& goto dosingle
if "%PICK%"=="32" set PROJPATH=DirtyPCBs\DirtyPCB_BoardStats\DirtyPCB_BoardStats.csproj& goto dosingle
if "%PICK%"=="33" set PROJPATH=DirtyPCBs\DirtyPCB_BoardRender\DirtyPCB_BoardRender.csproj& goto dosingle
if "%PICK%"=="34" set PROJPATH=DirtyPCBs\DirtyPCB_DXFStats\DirtyPCB_DXFStats.csproj& goto dosingle
if "%PICK%"=="35" set PROJPATH=DirtyPCBs\Base64Extractor\DirtyPCB_Base64Extractor.csproj& goto dosingle
if "%PICK%"=="36" set PROJPATH=DirtyPCBs\DirtyLocaleTest\DirtyPCB_LocaleTest.csproj& goto dosingle
if "%PICK%"=="37" set PROJPATH=GerberProjects\EagleLoadTest\EagleLoadTest.csproj& goto dosingle
if "%PICK%"=="38" set PROJPATH=GerberProjects\FrameCreatorTest\FrameCreatorTest.csproj& goto dosingle
echo  Invalid choice.
timeout /t 1 >nul
goto single

:dosingle
echo.
echo  Building: %PROJPATH%
"%DOTNET%" build "%ROOT%%PROJPATH%" -c Debug --nologo
echo.
pause
goto menu

REM =============================================
REM  Copy GUI
REM =============================================
:copygui
if not exist "%ROOT%%~1\bin\%CONFIG%\%~3\" exit /b 0
set DSTDIR=%OUT%\%~2\
if exist "%DSTDIR%" rmdir /s /q "%DSTDIR%"
mkdir "%DSTDIR%" 2>nul
xcopy "%ROOT%%~1\bin\%CONFIG%\%~3\*" "%DSTDIR%" /Y /Q /E >nul 2>&1
echo    %~2
exit /b 0

REM =============================================
REM  Copy CLI
REM =============================================
:copycli
if not exist "%ROOT%%~1\bin\%CONFIG%\%~2\" exit /b 0
xcopy "%ROOT%%~1\bin\%CONFIG%\%~2\*" "%CLI_OUT%\" /Y /Q >nul 2>&1
exit /b 0

:help
echo  Usage: build_all.bat
echo    Interactive menu - builds GerberTools.sln via dotnet build (fast, parallel)
echo    Option 1: Build ALL + copy outputs to Build\Output\
echo    Option 2: Build ALL Release
echo    Option 3: Build single project
goto end

:end
endlocal
