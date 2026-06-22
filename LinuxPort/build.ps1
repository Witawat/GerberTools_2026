param([string]$Config = "Debug")

$ErrorActionPreference = "Stop"
$ROOT = Split-Path -Parent $PSCommandPath
$OUT = "$ROOT\Output"
$CLI_OUT = "$OUT\CommandLine"
$COMBINED = "$ROOT\Combined"
$FW = "net48"

Write-Host "========================================"
Write-Host "  GerberTools LinuxPort Build [$Config]"
Write-Host "========================================"
Write-Host ""

# Clean
if (Test-Path $OUT) { Remove-Item -Recurse -Force $OUT }
if (Test-Path $COMBINED) { Remove-Item -Recurse -Force $COMBINED }
New-Item -ItemType Directory -Force $OUT, $CLI_OUT, $COMBINED | Out-Null

# Build
dotnet build "$ROOT\LinuxPort.sln" -c $Config --nologo
if ($LASTEXITCODE -ne 0) { exit 1 }

# Copy GUI
function Copy-Gui($src, $name) {
    $s = "$ROOT\$src\bin\$Config\$FW\*"
    $d = "$OUT\$name\"
    if (Test-Path $d) { Remove-Item -Recurse -Force $d }
    New-Item -ItemType Directory -Force $d | Out-Null
    Copy-Item $s $d -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "    -> Output\$name"
}

Write-Host "`nCopying GUI applications..."
Copy-Gui "QuickGerberRender" "QuickGerberRender"
Copy-Gui "GerberViewer" "GerberViewer"
Copy-Gui "GerberPanelizer" "GerberPanelizer"

# Combined
Write-Host "`nCopying to Combined..."
foreach ($app in @("QuickGerberRender","GerberViewer","GerberPanelizer")) {
    $s = "$OUT\$app\*"
    if (Test-Path $s) { Copy-Item $s "$COMBINED\" -Recurse -Force }
}
Write-Host "    -> Combined"

Write-Host "`n========================================"
Write-Host "  Build complete [$Config]"
Write-Host "========================================"
Write-Host "  Output: $OUT"
Write-Host "  Combined: $COMBINED"
