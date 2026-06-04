@echo off
setlocal enabledelayedexpansion
title GerberTools Build

set DOTNET=dotnet
set ROOT=%~dp0
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
echo    [3] Build Libraries only (Debug)
echo    [4] Build GUI Apps only (Debug)
echo    [5] Build CLI Tools only (Debug)
echo    [6] Build single project
echo    [Q] Quit
echo  ========================================
echo.
set /p CHOICE="  Enter choice [1-6/Q]: "

if /I "%CHOICE%"=="Q" goto end
if "%CHOICE%"=="1" (set CONFIG=Debug& set MODE=all& goto prepare)
if "%CHOICE%"=="2" (set CONFIG=Release& set MODE=all& goto prepare)
if "%CHOICE%"=="3" (set CONFIG=Debug& set MODE=lib& goto prepare)
if "%CHOICE%"=="4" (set CONFIG=Debug& set MODE=gui& goto prepare)
if "%CHOICE%"=="5" (set CONFIG=Debug& set MODE=cli& goto prepare)
if "%CHOICE%"=="6" (set CONFIG=Debug& set MODE=single& goto prepare)
echo  Invalid choice. Try again.
timeout /t 1 >nul
goto menu

REM =============================================
REM  Prepare output folders
REM =============================================
:prepare
echo.
if %MODE%==all (
    if exist "%OUT%" rmdir /s /q "%OUT%"
    mkdir "%OUT%" 2>nul
    mkdir "%CLI_OUT%" 2>nul
)
if %MODE%==gui (
    if exist "%OUT%" (
        for /d %%d in ("%OUT%\*") do (
            if /I not "%%~nxd"=="CommandLine" rmdir /s /q "%%d"
        )
    )
    mkdir "%OUT%" 2>nul
)
if %MODE%==cli (
    if exist "%CLI_OUT%" rmdir /s /q "%CLI_OUT%"
    mkdir "%CLI_OUT%" 2>nul
)
if %MODE%==single (
    mkdir "%OUT%" 2>nul
)

REM =============================================
REM  Build: Core Libraries
REM =============================================
if %MODE%==all goto build_all
if %MODE%==lib  goto build_lib
if %MODE%==gui  goto build_gui
if %MODE%==cli  goto build_cli
if %MODE%==single goto build_single
goto menu

REM =============================================
REM  BUILD ALL
REM =============================================
:build_all
call :section Libraries
call :build_all_libs
call :section "GUI Applications"
call :build_all_gui
call :section "CLI Tools"
call :build_all_cli
call :summary
goto ask_again

REM =============================================
REM  BUILD LIBS ONLY
REM =============================================
:build_lib
call :section Libraries
call :build_all_libs
call :summary
goto ask_again

REM =============================================
REM  BUILD GUI ONLY
REM =============================================
:build_gui
call :section "GUI Applications"
call :build_all_libs
call :build_all_gui
call :summary
goto ask_again

REM =============================================
REM  BUILD CLI ONLY
REM =============================================
:build_cli
call :section "CLI Tools"
call :build_all_libs
call :build_all_cli
call :summary
goto ask_again

REM =============================================
REM  BUILD SINGLE
REM =============================================
:build_single
echo.
echo  Available projects:
echo.
echo  --- GUI ---
echo   QuickGerberRender           GerberPanelizer             GerberViewer
echo   FitBitmapToOutlineAndMerge  CaseBuilder                 EagleBoardToCHeader
echo   FrontPanelBuilder           JLCDrop                     ProductionFrame
echo   VScorePanel                 PnP_Processor               SolderTool
echo   TINRS-ArtWorkGenerator      IconBuilder                 OpampCalculator
echo.
echo  --- CLI ---
echo   GerberAnalyse     GerberClipper      GerberCombiner
echo   GerberMover       GerberSanitize     GerberSubtract
echo   AutoPanelBuilder  AntennaBuilder     BOMConsolidator
echo   ProtoBoardGenerator LightPipeBuilder ImageToGerber
echo   MakeIcon          IconScanner        ReleaseBuilder
echo   SickOfBeige       BoardStats         BoardRender       DXFStats
echo   Base64Extractor   LocaleTest         EagleLoadTest     FrameCreatorTest
echo.
echo  Format: FolderName (no extension, no path)
echo.
set /p SINGLE="  Enter project folder name: "
if "%SINGLE%"=="" goto menu

REM Map friendly names to csproj paths
call :findproj "%SINGLE%"
if "%PROJPATH%"=="" (
    echo  Project not found: %SINGLE%
    timeout /t 2 >nul
    goto build_single
)
set CONFIG=Debug
call :build_all_libs
echo.
echo  ========== Building: %SINGLE% ==========
set BUILDFAIL=0
call :doproj "%PROJPATH%"
if %BUILDFAIL%==1 goto fail
echo  Build: %SINGLE% [OK]
goto ask_again

REM =============================================
REM  Build all libraries
REM =============================================
:build_all_libs
set BUILDFAIL=0
call :doproj "GerberLibrary\GerberLibrary.csproj"
if %BUILDFAIL%==1 exit /b 1
call :doproj "EagleLoaders\EagleLoaders.csproj"
if %BUILDFAIL%==1 exit /b 1
call :doproj "Project_Utilities\TilingLibrary\TINRS-ArtWork.csproj"
if %BUILDFAIL%==1 exit /b 1
call :doproj "GerberPanelizer\QuickFont\QuickFont.csproj"
if %BUILDFAIL%==1 exit /b 1
if %HAS_NET9%==1 (
    call :doproj "GerberLibrary.Core\GerberLibrary.Core.csproj"
    call :doproj "TilingLibrary.Core\TilingLibrary.Core.csproj"
)
exit /b 0

REM =============================================
REM  Build all GUI apps
REM =============================================
:build_all_gui
call :doproj "QuickGerberRender\QuickGerberRender.csproj"                     & if %BUILDFAIL%==0 call :copyout QuickGerberRender        QuickGerberRender           net48
call :doproj "GerberPanelizer\GerberPanelize.csproj"                          & if %BUILDFAIL%==0 call :copyout GerberPanelizer          GerberPanelizer             net48
call :doproj "GerberViewer\GerberViewer.csproj"                               & if %BUILDFAIL%==0 call :copyout GerberViewer             GerberViewer                net48
call :doproj "FitBitmapToOutlineAndMerge\FitBitmapToOutlineAndMerge.csproj"   & if %BUILDFAIL%==0 call :copyout FitBitmapToOutlineAndMerge FitBitmapToOutlineAndMerge net48
call :doproj "CaseBuilder\CaseBuilder.csproj"                                 & if %BUILDFAIL%==0 call :copyout CaseBuilder              CaseBuilder                 net48
call :doproj "EagleBoardToCHeader\EagleBoardToCHeader.csproj"                 & if %BUILDFAIL%==0 call :copyout EagleBoardToCHeader      EagleBoardToCHeader         net48
call :doproj "FrontPanelBuilder\FrontPanelBuilder.csproj"                     & if %BUILDFAIL%==0 call :copyout FrontPanelBuilder        FrontPanelBuilder           net48
call :doproj "JLCDrop\JLCDrop.csproj"                                         & if %BUILDFAIL%==0 call :copyout JLCDrop                  JLCDrop                     net48
call :doproj "VScorePanel\FrameDrop.csproj"                                   & if %BUILDFAIL%==0 call :copyout VScorePanel              FrameDrop                   net48
call :doproj "ProductionFrame\ProductionFrame.csproj"                         & if %BUILDFAIL%==0 call :copyout ProductionFrame          ProductionFrame             net48
call :doproj "PnP_Processor\PnP_Processor.csproj"                             & if %BUILDFAIL%==0 call :copyout PnP_Processor            PnP_Processor               net48
call :doproj "SolderTool\SolderTool.csproj"                                   & if %BUILDFAIL%==0 call :copyout SolderTool               SolderTool                  net48
call :doproj "TINRS-ArtWorkGenerator\TINRS-ArtWorkGenerator.csproj"           & if %BUILDFAIL%==0 call :copyout TINRS-ArtWorkGenerator   TINRS-ArtWorkGenerator      net48
call :doproj "Project_Utilities\IconBuilder\IconBuilder.csproj"               & if %BUILDFAIL%==0 call :copyout IconBuilder              IconBuilder                 net48
call :doproj "GerberProjects\OpampCalculator\OpampCalculator.csproj"          & if %BUILDFAIL%==0 call :copyout OpampCalculator          OpampCalculator             net48
if %HAS_NET9%==1 (
    call :doproj "GerberDrop\GerberDrop.csproj"                               & if %BUILDFAIL%==0 call :copyout GerberDrop               GerberDrop                  net9.0
    call :doproj "TiNRS-Tiler\TiNRS.Tiler.csproj"                             & if %BUILDFAIL%==0 call :copyout TiNRS-Tiler              TiNRS.Tiler                 net9.0
) else (
    echo   SKIPPED: GerberDrop, TiNRS-Tiler (net9.0)
)
exit /b 0

REM =============================================
REM  Build all CLI tools
REM =============================================
:build_all_cli
call :doproj "GerberAnalyse\GerberAnalyse.csproj"                     & if %BUILDFAIL%==0 call :copycli GerberAnalyse            net48
call :doproj "GerberClipper\GerberClipper.csproj"                     & if %BUILDFAIL%==0 call :copycli GerberClipper             net48
call :doproj "GerberCombiner\GerberCombiner.csproj"                   & if %BUILDFAIL%==0 call :copycli GerberCombiner            net48
call :doproj "GerberMover\GerberMover.csproj"                         & if %BUILDFAIL%==0 call :copycli GerberMover               net48
call :doproj "GerberSanitize\GerberSanitize.csproj"                   & if %BUILDFAIL%==0 call :copycli GerberSanitize             net48
call :doproj "GerberSubtract\GerberSubtract.csproj"                   & if %BUILDFAIL%==0 call :copycli GerberSubtract             net48
call :doproj "AutoPanelBuilder\AutoPanelBuilder.csproj"               & if %BUILDFAIL%==0 call :copycli AutoPanelBuilder          net48
call :doproj "AntennaBuilder\NFCAntennaBuilder.csproj"                & if %BUILDFAIL%==0 call :copycli AntennaBuilder            net48
call :doproj "BOMConsolidator\BOMConsolidator.csproj"                 & if %BUILDFAIL%==0 call :copycli BOMConsolidator           net48
call :doproj "ProtoBoardGenerator\ProtoBoardGenerator.csproj"         & if %BUILDFAIL%==0 call :copycli ProtoBoardGenerator       net48
call :doproj "LightPipeBuilder\LightPipeBuilder.csproj"               & if %BUILDFAIL%==0 call :copycli LightPipeBuilder           net48
call :doproj "ImageToGerber\FrontPanelImageToGerber.csproj"           & if %BUILDFAIL%==0 call :copycli ImageToGerber             net48
call :doproj "MakeIcon\MakeIcon.csproj"                               & if %BUILDFAIL%==0 call :copycli MakeIcon                  net48
call :doproj "Project_Utilities\IconScanner\IconScanner.csproj"       & if %BUILDFAIL%==0 call :copycli IconScanner               net48
call :doproj "Project_Utilities\ReleaseBuilder\ReleaseBuilder.csproj" & if %BUILDFAIL%==0 call :copycli ReleaseBuilder            net48
call :doproj "DirtyPCBs\SickOfBeige\DirtyPCB_SickOfBeige.csproj"      & if %BUILDFAIL%==0 call :copycli SickOfBeige               net48
call :doproj "DirtyPCBs\DirtyPCB_BoardStats\DirtyPCB_BoardStats.csproj"   & if %BUILDFAIL%==0 call :copycli BoardStats            net48
call :doproj "DirtyPCBs\DirtyPCB_BoardRender\DirtyPCB_BoardRender.csproj" & if %BUILDFAIL%==0 call :copycli BoardRender           net48
call :doproj "DirtyPCBs\DirtyPCB_DXFStats\DirtyPCB_DXFStats.csproj"       & if %BUILDFAIL%==0 call :copycli DXFStats              net48
call :doproj "DirtyPCBs\Base64Extractor\DirtyPCB_Base64Extractor.csproj"  & if %BUILDFAIL%==0 call :copycli Base64Extractor       net48
call :doproj "DirtyPCBs\DirtyLocaleTest\DirtyPCB_LocaleTest.csproj"       & if %BUILDFAIL%==0 call :copycli LocaleTest            net48
call :doproj "GerberProjects\EagleLoadTest\EagleLoadTest.csproj"          & if %BUILDFAIL%==0 call :copycli EagleLoadTest         net48
call :doproj "GerberProjects\FrameCreatorTest\FrameCreatorTest.csproj"    & if %BUILDFAIL%==0 call :copycli FrameCreatorTest      net48
if %HAS_NET9%==1 (
    call :doproj "GerberDebugger\GerberDebugger.csproj"               & if %BUILDFAIL%==0 call :copycli GerberDebugger            net9.0
    call :doproj "GerberSplitter\GerberSplitter.csproj"               & if %BUILDFAIL%==0 call :copycli GerberSplitter             net9.0
    call :doproj "GerberToDxf\GerberToDxf\GerberToDxf.csproj"         & if %BUILDFAIL%==0 call :copycli GerberToDxf               net9.0
    call :doproj "GerberToImage\GerberToImage.csproj"                 & if %BUILDFAIL%==0 call :copycli GerberToImage             net9.0
    call :doproj "GerberToOutline\GerberToOutline.csproj"             & if %BUILDFAIL%==0 call :copycli GerberToOutline           net9.0
    call :doproj "Tests\GerberTools.TestGenerator\GerberTools.TestGenerator.csproj" & if %BUILDFAIL%==0 call :copycli TestGenerator net9.0
    call :doproj "MigrationTest\MigrationTest.csproj"                 & if %BUILDFAIL%==0 call :copycli MigrationTest             net9.0
) else (
    echo   SKIPPED: 7 net9.0 CLI tools
)
exit /b 0

REM =============================================
REM  Helper: Section header
REM =============================================
:section
echo.
echo  ========== %~1 ==========
exit /b 0

REM =============================================
REM  Helper: Build a project (shows folder name, real-time output)
REM =============================================
:doproj
echo  [%CONFIG%] %~1
"%DOTNET%" build "%ROOT%%~1" -c %CONFIG% --nologo -v minimal 2>&1 | findstr /V "warning CS"
if %ERRORLEVEL%==0 set BUILDFAIL=0
if not %ERRORLEVEL%==0 (
    set BUILDFAIL=1
    echo  FAILED: %~1
)
exit /b 0

REM =============================================
REM  Helper: Copy GUI app output
REM =============================================
:copyout
set SRCDIR=%ROOT%%~1\bin\%CONFIG%\%~3\
set DSTDIR=%OUT%\%~2\
if exist "%DSTDIR%" rmdir /s /q "%DSTDIR%"
mkdir "%DSTDIR%" 2>nul
xcopy "%SRCDIR%*" "%DSTDIR%" /Y /Q /E >nul 2>&1
echo    -> %OUT%%~2
exit /b 0

REM =============================================
REM  Helper: Copy CLI tool output
REM =============================================
:copycli
set SRCDIR=%ROOT%%~1\bin\%CONFIG%\%~2\
xcopy "%SRCDIR%*" "%CLI_OUT%\" /Y /Q >nul 2>&1
echo    -> %CLI_OUT%
exit /b 0

REM =============================================
REM  Helper: Find project path for single build
REM =============================================
:findproj
if /I "%~1"=="QuickGerberRender"           set PROJPATH=QuickGerberRender\QuickGerberRender.csproj& exit /b 0
if /I "%~1"=="GerberPanelizer"             set PROJPATH=GerberPanelizer\GerberPanelize.csproj& exit /b 0
if /I "%~1"=="GerberViewer"                set PROJPATH=GerberViewer\GerberViewer.csproj& exit /b 0
if /I "%~1"=="FitBitmapToOutlineAndMerge"   set PROJPATH=FitBitmapToOutlineAndMerge\FitBitmapToOutlineAndMerge.csproj& exit /b 0
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
REM --- CLI ---
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

REM =============================================
REM  Summary
REM =============================================
:summary
echo.
echo  ========================================
echo    Build complete  [%CONFIG%]
echo  ========================================
echo    Output: %OUT%
echo.
if exist "%OUT%" (
    echo  --- GUI Applications ---
    for /d %%d in ("%OUT%\*") do if /I not "%%~nxd"=="CommandLine" echo    %%~nxd
)
if exist "%CLI_OUT%" (
    echo.
    echo  --- CLI Tools in CommandLine\ ---
)
echo.
exit /b 0

REM =============================================
:ask_again
echo.
set /p AGAIN="  Build again? [Y/N]: "
if /I "%AGAIN%"=="Y" goto menu
goto end

REM =============================================
:fail
echo.
echo  ========================================
echo    BUILD FAILED
echo  ========================================
pause
goto end

:help
echo  Usage: build_all.bat
echo.
echo    Interactive menu - choose Debug/Release, build all or specific projects.
echo.
echo    build_all.bat --help   Show this help
goto end

:end
endlocal
