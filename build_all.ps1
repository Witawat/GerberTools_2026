param(
    [string]$Config = "Debug"
)

$ErrorActionPreference = "Stop"
$ROOT = Resolve-Path "$PSScriptRoot\"
$OUT = "$ROOT\Build\Output"
$CLI_OUT = "$OUT\CommandLine"

# Check .NET SDK
$sdkVer = dotnet --version 2>$null
$hasNet9 = ($sdkVer -match "^[0-9]+" -and [int]$Matches[0] -ge 9)

Write-Host "========================================"
Write-Host "  GerberTools Build Script [$Config]"
Write-Host "========================================"
if (-not $hasNet9) {
    Write-Host ".NET SDK $sdkVer - net9.0 projects will be SKIPPED"
}
Write-Host ""

# Clean output
if (Test-Path $OUT) { Remove-Item -Recurse -Force $OUT }
New-Item -ItemType Directory -Force $OUT | Out-Null
New-Item -ItemType Directory -Force $CLI_OUT | Out-Null

function Build($proj) {
    Write-Host "  Building: $proj"
    dotnet build "$ROOT\$proj" -c $Config --nologo -v q 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  FAILED: $proj" -ForegroundColor Red
        dotnet build "$ROOT\$proj" -c $Config --nologo 2>&1 | Select-String "error"
        exit 1
    }
}

function CopyOut($src, $dest, $fw) {
    $s = "$ROOT\$src\bin\$Config\$fw\*"
    $d = "$OUT\$dest\"
    if (Test-Path $d) { Remove-Item -Recurse -Force $d }
    New-Item -ItemType Directory -Force $d | Out-Null
    Copy-Item $s $d -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
    Write-Host "    -> $OUT$dest"
}

function CopyCli($src, $fw) {
    $s = "$ROOT\$src\bin\$Config\$fw\*"
    Copy-Item $s $CLI_OUT\ -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
    Write-Host "    -> $CLI_OUT"
}

# =========================================
Write-Host "[$Config] Core Libraries..."
Build "GerberLibrary\GerberLibrary.csproj"
Build "EagleLoaders\EagleLoaders.csproj"
Build "Project_Utilities\TilingLibrary\TINRS-ArtWork.csproj"
Build "GerberPanelizer\QuickFont\QuickFont.csproj"
if ($hasNet9) {
    Build "GerberLibrary.Core\GerberLibrary.Core.csproj"
    Build "TilingLibrary.Core\TilingLibrary.Core.csproj"
}
Write-Host "  [OK]`n"

# =========================================
Write-Host "[$Config] GUI Applications..."

$gui = @(
    @{p="QuickGerberRender\QuickGerberRender.csproj";                     o="QuickGerberRender";         s="QuickGerberRender";         f="net48"},
    @{p="GerberPanelizer\GerberPanelize.csproj";                          o="GerberPanelizer";           s="GerberPanelizer";           f="net48"},
    @{p="GerberViewer\GerberViewer.csproj";                               o="GerberViewer";              s="GerberViewer";              f="net48"},
    @{p="FitBitmapToOutlineAndMerge\FitBitmapToOutlineAndMerge.csproj";   o="FitBitmapToOutlineAndMerge";s="FitBitmapToOutlineAndMerge";f="net48"},
    @{p="CaseBuilder\CaseBuilder.csproj";                                 o="CaseBuilder";               s="CaseBuilder";               f="net48"},
    @{p="EagleBoardToCHeader\EagleBoardToCHeader.csproj";                o="EagleBoardToCHeader";       s="EagleBoardToCHeader";       f="net48"},
    @{p="FrontPanelBuilder\FrontPanelBuilder.csproj";                    o="FrontPanelBuilder";         s="FrontPanelBuilder";         f="net48"},
    @{p="JLCDrop\JLCDrop.csproj";                                         o="JLCDrop";                   s="JLCDrop";                   f="net48"},
    @{p="VScorePanel\FrameDrop.csproj";                                   o="FrameDrop";                 s="VScorePanel";               f="net48"},
    @{p="ProductionFrame\ProductionFrame.csproj";                         o="ProductionFrame";           s="ProductionFrame";           f="net48"},
    @{p="PnP_Processor\PnP_Processor.csproj";                             o="PnP_Processor";             s="PnP_Processor";             f="net48"},
    @{p="SolderTool\SolderTool.csproj";                                   o="SolderTool";                s="SolderTool";                f="net48"},
    @{p="TINRS-ArtWorkGenerator\TINRS-ArtWorkGenerator.csproj";           o="TINRS-ArtWorkGenerator";   s="TINRS-ArtWorkGenerator";     f="net48"},
    @{p="Project_Utilities\IconBuilder\IconBuilder.csproj";               o="IconBuilder";               s="Project_Utilities\IconBuilder"; f="net48"},
    @{p="GerberProjects\OpampCalculator\OpampCalculator.csproj";          o="OpampCalculator";           s="GerberProjects\OpampCalculator"; f="net48"}
)

if ($hasNet9) {
    $gui += @(
        @{p="GerberDrop\GerberDrop.csproj";                               o="GerberDrop";                s="GerberDrop";                f="net9.0"},
        @{p="TiNRS-Tiler\TiNRS.Tiler.csproj";                              o="TiNRS-Tiler";               s="TiNRS-Tiler";               f="net9.0"}
    )
} else {
    Write-Host "  SKIPPED: GerberDrop, TiNRS-Tiler (net9.0)"
}

foreach ($g in $gui) {
    Build $g.p
    CopyOut $g.s $g.o $g.f
}
Write-Host "  [OK]`n"

# =========================================
Write-Host "[$Config] CLI Tools..."

$cli = @(
    @{p="GerberAnalyse\GerberAnalyse.csproj";                                  s="GerberAnalyse";                 f="net48"},
    @{p="GerberClipper\GerberClipper.csproj";                                  s="GerberClipper";                 f="net48"},
    @{p="GerberCombiner\GerberCombiner.csproj";                                s="GerberCombiner";                f="net48"},
    @{p="GerberMover\GerberMover.csproj";                                      s="GerberMover";                   f="net48"},
    @{p="GerberSanitize\GerberSanitize.csproj";                                s="GerberSanitize";                 f="net48"},
    @{p="GerberSubtract\GerberSubtract.csproj";                                s="GerberSubtract";                f="net48"},
    @{p="AutoPanelBuilder\AutoPanelBuilder.csproj";                            s="AutoPanelBuilder";              f="net48"},
    @{p="AntennaBuilder\NFCAntennaBuilder.csproj";                             s="AntennaBuilder";                f="net48"},
    @{p="BOMConsolidator\BOMConsolidator.csproj";                              s="BOMConsolidator";               f="net48"},
    @{p="ProtoBoardGenerator\ProtoBoardGenerator.csproj";                      s="ProtoBoardGenerator";           f="net48"},
    @{p="LightPipeBuilder\LightPipeBuilder.csproj";                             s="LightPipeBuilder";              f="net48"},
    @{p="ImageToGerber\FrontPanelImageToGerber.csproj";                        s="ImageToGerber";                 f="net48"},
    @{p="MakeIcon\MakeIcon.csproj";                                            s="MakeIcon";                      f="net48"},
    @{p="Project_Utilities\IconScanner\IconScanner.csproj";                   s="Project_Utilities\IconScanner"; f="net48"},
    @{p="Project_Utilities\ReleaseBuilder\ReleaseBuilder.csproj";             s="Project_Utilities\ReleaseBuilder"; f="net48"},
    @{p="DirtyPCBs\SickOfBeige\DirtyPCB_SickOfBeige.csproj";                   s="DirtyPCBs\SickOfBeige";         f="net48"},
    @{p="DirtyPCBs\DirtyPCB_BoardStats\DirtyPCB_BoardStats.csproj";           s="DirtyPCBs\DirtyPCB_BoardStats"; f="net48"},
    @{p="DirtyPCBs\DirtyPCB_BoardRender\DirtyPCB_BoardRender.csproj";         s="DirtyPCBs\DirtyPCB_BoardRender"; f="net48"},
    @{p="DirtyPCBs\DirtyPCB_DXFStats\DirtyPCB_DXFStats.csproj";               s="DirtyPCBs\DirtyPCB_DXFStats";   f="net48"},
    @{p="DirtyPCBs\Base64Extractor\DirtyPCB_Base64Extractor.csproj";          s="DirtyPCBs\Base64Extractor";     f="net48"},
    @{p="DirtyPCBs\DirtyLocaleTest\DirtyPCB_LocaleTest.csproj";               s="DirtyPCBs\DirtyLocaleTest";     f="net48"},
    @{p="GerberProjects\EagleLoadTest\EagleLoadTest.csproj";                  s="GerberProjects\EagleLoadTest";  f="net48"},
    @{p="GerberProjects\FrameCreatorTest\FrameCreatorTest.csproj";            s="GerberProjects\FrameCreatorTest"; f="net48"}
)

if ($hasNet9) {
    $cli += @(
        @{p="GerberDebugger\GerberDebugger.csproj";                            s="GerberDebugger";                f="net9.0"},
        @{p="GerberSplitter\GerberSplitter.csproj";                            s="GerberSplitter";                f="net9.0"},
        @{p="GerberToDxf\GerberToDxf\GerberToDxf.csproj";                      s="GerberToDxf\GerberToDxf";        f="net9.0"},
        @{p="GerberToImage\GerberToImage.csproj";                              s="GerberToImage";                 f="net9.0"},
        @{p="GerberToOutline\GerberToOutline.csproj";                          s="GerberToOutline";               f="net9.0"},
        @{p="Tests\GerberTools.TestGenerator\GerberTools.TestGenerator.csproj"; s="Tests\GerberTools.TestGenerator"; f="net9.0"},
        @{p="MigrationTest\MigrationTest.csproj";                              s="MigrationTest";                 f="net9.0"}
    )
} else {
    Write-Host "  SKIPPED: 7 net9.0 CLI tools"
}

foreach ($c in $cli) {
    Build $c.p
    CopyCli $c.s $c.f
}
Write-Host "  [OK]`n"

# =========================================
$guiCount = (Get-ChildItem $OUT -Directory | Where-Object { $_.Name -ne "CommandLine" }).Count
$cliCount = (Get-ChildItem $CLI_OUT -File).Count

Write-Host "========================================"
Write-Host "  Build Complete!  GUI: $guiCount  CLI: $cliCount"
Write-Host "========================================"
Write-Host "Output: $OUT"
Get-ChildItem $OUT -Directory | ForEach-Object { Write-Host "  $_" }
