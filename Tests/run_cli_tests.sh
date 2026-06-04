#!/bin/bash
set -e

# Setup Test Environment
TEST_DIR="Tests/GerberTools.TestGenerator/TestFiles"
GENERATOR_PROJ="Tests/GerberTools.TestGenerator/GerberTools.TestGenerator.csproj"
GERBER_TO_IMAGE_DLL="GerberToImage/bin/Debug/net9.0/GerberToImage.dll"
GERBER_TO_DXF_DLL="GerberToDxf/GerberToDxf/bin/Debug/net9.0/GerberToDxf.dll"
GERBER_SPLITTER_DLL="GerberSplitter/bin/Debug/net9.0/GerberSplitter.dll"
GERBER_TO_OUTLINE_DLL="GerberToOutline/bin/Debug/net9.0/GerberToOutline.dll"

echo "=== 1. Generating Test Data ==="
dotnet run --project "$GENERATOR_PROJ" -- "$TEST_DIR"

echo "=== 2. Building CLI Tools ==="
dotnet build GerberToImage/GerberToImage.csproj -v q
dotnet build GerberToDxf/GerberToDxf/GerberToDxf.csproj -v q
dotnet build GerberSplitter/GerberSplitter.csproj -v q
dotnet build GerberToOutline/GerberToOutline.csproj -v q

echo "=== 3. Testing GerberToImage ==="
# Run against the test directory
echo "Running: dotnet $GERBER_TO_IMAGE_DLL $TEST_DIR --dpi 200 --noxray"
dotnet "$GERBER_TO_IMAGE_DLL" "$TEST_DIR" --dpi 200 --noxray

# Verify output
IMAGE_OUTPUT="$TEST_DIR""_Combined_Top.png"
if [ -f "$IMAGE_OUTPUT" ]; then
    echo "✅ GerberToImage Success: Created $IMAGE_OUTPUT"
else
    echo "❌ GerberToImage Failed: Output file $IMAGE_OUTPUT not found"
    exit 1
fi

# Run with custom colors and nopcb
echo "Running: dotnet $GERBER_TO_IMAGE_DLL $TEST_DIR --dpi 100 --nopcb --silk red --copper blue --mask yellow --trace green"
dotnet "$GERBER_TO_IMAGE_DLL" "$TEST_DIR" --dpi 100 --nopcb --silk red --copper blue --mask yellow --trace green

echo "=== 4. Testing GerberToDxf ==="
# Determine input and output paths (using absolute or relative correctly)
INPUT_GKO="$TEST_DIR/board_outline.gko"
OUTPUT_DXF="$TEST_DIR/board_outline.dxf"

echo "Running: dotnet $GERBER_TO_DXF_DLL $INPUT_GKO $OUTPUT_DXF"
dotnet "$GERBER_TO_DXF_DLL" "$INPUT_GKO" "$OUTPUT_DXF"

# Verify output
if [ -f "$OUTPUT_DXF" ]; then
    echo "✅ GerberToDxf Success: Created $OUTPUT_DXF"
else
    echo "❌ GerberToDxf Failed: Output file $OUTPUT_DXF not found"
    exit 1
fi

# Test with flags
OUTPUT_DXF_FLAGS="$TEST_DIR/board_outline_flags.dxf"
echo "Running: dotnet $GERBER_TO_DXF_DLL $INPUT_GKO $OUTPUT_DXF_FLAGS -nooutline -nodisplay"
dotnet "$GERBER_TO_DXF_DLL" "$INPUT_GKO" "$OUTPUT_DXF_FLAGS" -nooutline -nodisplay

if [ -f "$OUTPUT_DXF_FLAGS" ]; then
    echo "✅ GerberToDxf Flags Success: Created $OUTPUT_DXF_FLAGS"
else
    echo "❌ GerberToDxf Flags Failed: Output file $OUTPUT_DXF_FLAGS not found"
    exit 1
fi

echo "=== 5. Testing GerberSplitter ==="
INPUT_GKO="$TEST_DIR/board_outline.gko"
INPUT_COPPER="$TEST_DIR/top_copper.gtl"
INPUT_BOT_COPPER="$TEST_DIR/bottom_copper.gbl"
INPUT_TOP_SILK="$TEST_DIR/top_silk.gto"
INPUT_BOT_SILK="$TEST_DIR/bottom_silk.gbo"

echo "Running Spliter..."
dotnet "$GERBER_SPLITTER_DLL" "$INPUT_GKO" "$INPUT_COPPER" "$INPUT_BOT_COPPER" "$INPUT_TOP_SILK" "$INPUT_BOT_SILK"

# Verify outputs
EXPECTED_FILES=(
    "$TEST_DIR/Output/board_outline/Slice1/top_copper.gtl"
    "$TEST_DIR/Output/board_outline/Slice1/bottom_copper.gbl"
    "$TEST_DIR/Output/board_outline/Slice1/top_silk.gto"
    "$TEST_DIR/Output/board_outline/Slice1/bottom_silk.gbo"
)

for OUTPUT_FILE in "${EXPECTED_FILES[@]}"; do
    if [ -f "$OUTPUT_FILE" ]; then
        echo "✅ GerberSplitter Success: Created $OUTPUT_FILE"
    else
        echo "❌ GerberSplitter Failed: Output file $OUTPUT_FILE not found"
        exit 1
    fi
done

echo "=== 6. Testing GerberToOutline ==="
INPUT_GKO="$TEST_DIR/board_outline.gko"
OUTPUT_SVG="$TEST_DIR/board_outline.svg"
echo "Running GerberToOutline..."
dotnet "$GERBER_TO_OUTLINE_DLL" "$INPUT_GKO" "$OUTPUT_SVG"

if [ -f "$OUTPUT_SVG" ]; then
    echo "✅ GerberToOutline Success: Created $OUTPUT_SVG"
else
    echo "❌ GerberToOutline Failed: Output file $OUTPUT_SVG not found"
    exit 1
fi

echo "=== 7. Ucamco Test Suite ==="
UCAMCO_DIR="Tests/Ucamco_TestFiles"
UCAMCO_URL="https://www.ucamco.com/files/downloads/file_en/423/gerber-layer-format-test-files_en.zip?b5cf9a44b967ee55d3e962cfb51f64b0"
UCAMCO_ZIP="$UCAMCO_DIR/ucamco_tests.zip"

mkdir -p "$UCAMCO_DIR"

if [ ! -f "$UCAMCO_ZIP" ]; then
    echo "Downloading Ucamco test files..."
    curl -L -o "$UCAMCO_ZIP" "$UCAMCO_URL"
fi

echo "Unzipping test files..."
unzip -o -q "$UCAMCO_ZIP" -d "$UCAMCO_DIR"

echo "Running GerberToImage on Ucamco files..."
# Find all gerber-ish files (assuming .gbr, .grb, etc or just iterate known extensions if unsure)
# Looking at typical Ucamco zips, they have folders. We should find files recursively.
find "$UCAMCO_DIR" -type f \( -name "*.gbr" -o -name "*.grb" -o -name "*.gbx" \) | while read -r FILE; do
    echo "Processing $FILE..."
    dotnet "$GERBER_TO_IMAGE_DLL" "$FILE" --dpi 400
done

echo "=== Test Suite Completed Successfully ==="
echo "All tools ran and produced output on the current platform."
