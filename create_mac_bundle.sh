#!/bin/bash
set -e

PROJECT_NAME="TiNRS-Tiler"
CSPROJ_PATH="TiNRS-Tiler/TiNRS.Tiler.csproj"
APP_NAME="TiNRS-Tiler.app"
PUBLISH_DIR="publish_output"

# Detect Architecture
ARCH=$(uname -m)
if [ "$ARCH" == "arm64" ]; then
    RID="osx-arm64"
    HOMEBREW_PREFIX="/opt/homebrew"
else
    RID="osx-x64"
    HOMEBREW_PREFIX="/usr/local"
fi

echo "🔹 Building for $RID..."

# Publish Self-Contained
dotnet publish "$CSPROJ_PATH" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:UseAppHost=true \
    -o "$PUBLISH_DIR"

# Create App Bundle Structure
echo "🔹 Creating App Bundle: $APP_NAME..."
rm -rf "$APP_NAME"
mkdir -p "$APP_NAME/Contents/MacOS"
mkdir -p "$APP_NAME/Contents/Resources"

# Copy Published Files
cp -a "$PUBLISH_DIR/"* "$APP_NAME/Contents/MacOS/"

# Bundle libgdiplus
echo "🔹 Bundling Native Dependencies..."
GDI_LIB="$HOMEBREW_PREFIX/opt/mono-libgdiplus/lib/libgdiplus.dylib"

if [ -f "$GDI_LIB" ]; then
    cp "$GDI_LIB" "$APP_NAME/Contents/MacOS/"
    # Create copies for various lookup names to be safe
    cp "$GDI_LIB" "$APP_NAME/Contents/MacOS/libgdiplus"
    cp "$GDI_LIB" "$APP_NAME/Contents/MacOS/gdiplus.dll"
    echo "✅ specific libgdiplus bundled."
else
    echo "⚠️  WARNING: libgdiplus not found at $GDI_LIB"
fi

# Create Info.plist
echo "🔹 Creating Info.plist..."
cat > "$APP_NAME/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>TiNRS Tiler</string>
    <key>CFBundleDisplayName</key>
    <string>TiNRS Tiler</string>
    <key>CFBundleIdentifier</key>
    <string>com.tinrs.tiler</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>????</string>
    <key>CFBundleExecutable</key>
    <string>TiNRS.Tiler</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOF

# Cleanup
rm -rf "$PUBLISH_DIR"

echo "✅ App Bundle Created: $APP_NAME"
echo "👉 You can run it with: open $APP_NAME"
