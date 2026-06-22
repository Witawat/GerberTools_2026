#!/bin/bash
set -euo pipefail

# Linux build script for GerberTools Mono port
# ใช้งาน: ./build.sh [Debug|Release]
#   - Argument 1: Configuration (Debug/Release, default: Debug)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

CONFIG="${1:-Debug}"
TARGET_FW="net48"
OUT_DIR="$SCRIPT_DIR/Output"
CLI_OUT_DIR="$OUT_DIR/CommandLine"
COMBINED_DIR="$SCRIPT_DIR/Combined"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# =========================================
# Helper functions
# =========================================

clean_output() {
    echo "  Cleaning old output..."
    rm -rf "$OUT_DIR" "$COMBINED_DIR"
    mkdir -p "$OUT_DIR" "$CLI_OUT_DIR" "$COMBINED_DIR"
}

build_proj() {
    local proj="$1"
    echo -e "  Building: ${YELLOW}$proj${NC}"
    if ! dotnet build "$proj" -c "$CONFIG" --nologo -v q 2>/dev/null; then
        echo -e "  ${RED}FAILED: $proj${NC}"
        dotnet build "$proj" -c "$CONFIG" --nologo 2>&1 | grep -E "error "
        exit 1
    fi
}

copy_gui() {
    local src_dir="$1"
    local app_name="$2"
    local src_path="$SCRIPT_DIR/$src_dir/bin/$CONFIG/$TARGET_FW"
    local dst_path="$OUT_DIR/$app_name"

    if [ ! -d "$src_path" ]; then
        echo -e "  ${YELLOW}Warning: $src_path not found, skipping${NC}"
        return
    fi
    rm -rf "$dst_path"
    mkdir -p "$dst_path"
    cp -r "$src_path/"* "$dst_path/"
    echo -e "    -> ${GREEN}Output/$app_name${NC}"
}

copy_cli() {
    local src_dir="$1"
    local src_path="$SCRIPT_DIR/$src_dir/bin/$CONFIG/$TARGET_FW"

    if [ ! -d "$src_path" ]; then
        return
    fi
    cp -r "$src_path/"* "$CLI_OUT_DIR/"
}

# =========================================
# Main build
# =========================================

echo ""
echo "========================================"
echo "  GerberTools Linux Port Build [$CONFIG]"
echo "========================================"
echo ""

clean_output

echo "  Building all projects..."
echo ""

# Libraries (dependency order)
build_proj "GerberLibrary/GerberLibrary.csproj"
build_proj "EagleLoaders/EagleLoaders.csproj"

# GUI applications
build_proj "QuickGerberRender/QuickGerberRender.csproj"
build_proj "GerberViewer/GerberViewer.csproj"
build_proj "GerberPanelizer/GerberPanelize.csproj"

echo ""
echo -e "  ${GREEN}All builds passed${NC}"
echo ""

# ---- Copy outputs ----
echo "  Copying GUI applications..."
echo ""
copy_gui "QuickGerberRender" "QuickGerberRender"
copy_gui "GerberViewer" "GerberViewer"
copy_gui "GerberPanelizer" "GerberPanelizer"
echo ""

# Combined folder (3 main apps)
echo "  Copying to Combined folder..."
for app in QuickGerberRender GerberViewer GerberPanelizer; do
    src="$OUT_DIR/$app"
    if [ -d "$src" ]; then
        cp -r "$src/"* "$COMBINED_DIR/"
    fi
done
echo -e "    -> ${GREEN}Combined${NC}"
echo ""

# =========================================
# Summary
# =========================================
GUI_COUNT=$(find "$OUT_DIR" -mindepth 1 -maxdepth 1 -type d ! -name "CommandLine" | wc -l)

echo "========================================"
echo -e "  ${GREEN}Build complete [$CONFIG]${NC}"
echo "========================================"
echo "  Output: $OUT_DIR"
echo ""
for d in "$OUT_DIR"/*/; do
    name="$(basename "$d")"
    [ "$name" = "CommandLine" ] && continue
    echo "    $name"
done
echo ""
echo "  รัน:"
echo "    mono Output/QuickGerberRender/QuickGerberRender.exe"
echo "    mono Output/GerberViewer/GerberViewer.exe"
echo "    mono Output/GerberPanelizer/GerberPanelizer.exe"
echo ""
