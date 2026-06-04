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
echo.
echo  Available projects (folder name):
echo  QuickGerberRender CaseBuilder EagleBoardToCHeader FrontPanelBuilder
echo  JLCDrop VScorePanel ProductionFrame PnP_Processor SolderTool
echo  TINRS-ArtWorkGenerator IconBuilder OpampCalculator FitBitmapToOutlineAndMerge
echo  GerberAnalyse GerberClipper GerberCombiner GerberMover
echo  GerberSanitize GerberSubtract AutoPanelBuilder AntennaBuilder
echo  BOMConsolidator ProtoBoardGenerator LightPipeBuilder ImageToGerber
echo  MakeIcon IconScanner ReleaseBuilder
echo  SickOfBeige BoardStats BoardRender DXFStats Base64Extractor LocaleTest
echo  EagleLoadTest FrameCreatorTest
echo.
set /p SINGLE="  Enter project folder: "
if "%SINGLE%"=="" goto menu
call :findproj "%SINGLE%"
if "%PROJPATH%"=="" (
    echo  Not found: %SINGLE%
    timeout /t 2 >nul
    goto single
)
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

REM =============================================
REM  Find project for single build
REM =============================================
:findproj
if /I "%~1"=="QuickGerberRender"           set PROJPATH=QuickGerberRender\QuickGerberRender.csproj& exit /b 0
if /I "%~1"=="CaseBuilder"                set PROJPATH=CaseBuilder\CaseBuilder.csproj& exit /b 0
if /I "%~1"=="EagleBoardToCHeader"        set PROJPATH=EagleBoardToCHeader\EagleBoardToCHeader.csproj& exit /b 0
if /I "%~1"=="FrontPanelBuilder"          set PROJPATH=FrontPanelBuilder\FrontPanelBuilder.csproj& exit /b 0
if /I "%~1"=="JLCDrop"                   set PROJPATH=JLCDrop\JLCDrop.csproj& exit /b 0
if /I "%~1"=="VScorePanel"               set PROJPATH=VScorePanel\FrameDrop.csproj& exit /b 0
if /I "%~1"=="ProductionFrame"           set PROJPATH=ProductionFrame\ProductionFrame.csproj& exit /b 0
if /I "%~1"=="PnP_Processor"            set PROJPATH=PnP_Processor\PnP_Processor.csproj& exit /b 0
if /I "%~1"=="SolderTool"               set PROJPATH=SolderTool\SolderTool.csproj& exit /b 0
if /I "%~1"=="TINRS-ArtWorkGenerator"    set PROJPATH=TINRS-ArtWorkGenerator\TINRS-ArtWorkGenerator.csproj& exit /b 0
if /I "%~1"=="IconBuilder"              set PROJPATH=Project_Utilities\IconBuilder\IconBuilder.csproj& exit /b 0
if /I "%~1"=="OpampCalculator"          set PROJPATH=GerberProjects\OpampCalculator\OpampCalculator.csproj& exit /b 0
if /I "%~1"=="FitBitmapToOutlineAndMerge" set PROJPATH=FitBitmapToOutlineAndMerge\FitBitmapToOutlineAndMerge.csproj& exit /b 0
if /I "%~1"=="GerberAnalyse"            set PROJPATH=GerberAnalyse\GerberAnalyse.csproj& exit /b 0
if /I "%~1"=="GerberClipper"            set PROJPATH=GerberClipper\GerberClipper.csproj& exit /b 0
if /I "%~1"=="GerberCombiner"           set PROJPATH=GerberCombiner\GerberCombiner.csproj& exit /b 0
if /I "%~1"=="GerberMover"             set PROJPATH=GerberMover\GerberMover.csproj& exit /b 0
if /I "%~1"=="GerberSanitize"           set PROJPATH=GerberSanitize\GerberSanitize.csproj& exit /b 0
if /I "%~1"=="GerberSubtract"           set PROJPATH=GerberSubtract\GerberSubtract.csproj& exit /b 0
if /I "%~1"=="AutoPanelBuilder"         set PROJPATH=AutoPanelBuilder\AutoPanelBuilder.csproj& exit /b 0
if /I "%~1"=="AntennaBuilder"          set PROJPATH=AntennaBuilder\NFCAntennaBuilder.csproj& exit /b 0
if /I "%~1"=="BOMConsolidator"         set PROJPATH=BOMConsolidator\BOMConsolidator.csproj& exit /b 0
if /I "%~1"=="ProtoBoardGenerator"      set PROJPATH=ProtoBoardGenerator\ProtoBoardGenerator.csproj& exit /b 0
if /I "%~1"=="LightPipeBuilder"         set PROJPATH=LightPipeBuilder\LightPipeBuilder.csproj& exit /b 0
if /I "%~1"=="ImageToGerber"           set PROJPATH=ImageToGerber\FrontPanelImageToGerber.csproj& exit /b 0
if /I "%~1"=="MakeIcon"               set PROJPATH=MakeIcon\MakeIcon.csproj& exit /b 0
if /I "%~1"=="IconScanner"            set PROJPATH=Project_Utilities\IconScanner\IconScanner.csproj& exit /b 0
if /I "%~1"=="ReleaseBuilder"          set PROJPATH=Project_Utilities\ReleaseBuilder\ReleaseBuilder.csproj& exit /b 0
if /I "%~1"=="SickOfBeige"            set PROJPATH=DirtyPCBs\SickOfBeige\DirtyPCB_SickOfBeige.csproj& exit /b 0
if /I "%~1"=="BoardStats"             set PROJPATH=DirtyPCBs\DirtyPCB_BoardStats\DirtyPCB_BoardStats.csproj& exit /b 0
if /I "%~1"=="BoardRender"            set PROJPATH=DirtyPCBs\DirtyPCB_BoardRender\DirtyPCB_BoardRender.csproj& exit /b 0
if /I "%~1"=="DXFStats"              set PROJPATH=DirtyPCBs\DirtyPCB_DXFStats\DirtyPCB_DXFStats.csproj& exit /b 0
if /I "%~1"=="Base64Extractor"        set PROJPATH=DirtyPCBs\Base64Extractor\DirtyPCB_Base64Extractor.csproj& exit /b 0
if /I "%~1"=="LocaleTest"            set PROJPATH=DirtyPCBs\DirtyLocaleTest\DirtyPCB_LocaleTest.csproj& exit /b 0
if /I "%~1"=="EagleLoadTest"          set PROJPATH=GerberProjects\EagleLoadTest\EagleLoadTest.csproj& exit /b 0
if /I "%~1"=="FrameCreatorTest"       set PROJPATH=GerberProjects\FrameCreatorTest\FrameCreatorTest.csproj& exit /b 0
set PROJPATH=
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
