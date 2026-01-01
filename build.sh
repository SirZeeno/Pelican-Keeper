#!/bin/bash

# Pelican Keeper Build Script
# Usage: ./build.sh [platform] [configuration]
# Platforms: linux, windows, osx, all (default: current)
# Configuration: debug, release (default: release)
# Version is auto-detected from latest git tag

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="Pelican Keeper/Pelican Keeper.csproj"
OUTPUT_DIR="$SCRIPT_DIR/dist"

# Parse arguments
PLATFORM="${1:-current}"
CONFIG="${2:-release}"

# Get version from latest git tag (strips 'v' prefix)
VERSION=$(git describe --tags --abbrev=0 2>/dev/null | sed 's/^v//' || echo "0.0.0-dev")
echo "📦 Version: $VERSION"

# Normalize configuration
CONFIG=$(echo "$CONFIG" | tr '[:upper:]' '[:lower:]')
if [[ "$CONFIG" == "release" ]]; then
    CONFIG_FLAG="Release"
else
    CONFIG_FLAG="Debug"
fi

# Platform mappings
declare -A RIDS=(
    ["linux"]="linux-x64"
    ["windows"]="win-x64"
    ["osx"]="osx-x64"
)

build_platform() {
    local rid=$1
    local output_name="Pelican-Keeper-$rid"
    
    echo "🔨 Building for $rid ($CONFIG_FLAG)..."
    
    dotnet publish "$SCRIPT_DIR/$PROJECT" \
        -c "$CONFIG_FLAG" \
        -r "$rid" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:Version="$VERSION" \
        -o "$OUTPUT_DIR/$output_name"
    
    echo "✅ Built: $OUTPUT_DIR/$output_name"
}

# Clean previous builds
if [[ -d "$OUTPUT_DIR" ]]; then
    echo "🧹 Cleaning previous builds..."
    rm -rf "$OUTPUT_DIR"
fi
mkdir -p "$OUTPUT_DIR"

case "$PLATFORM" in
    "current")
        # Detect current platform
        case "$(uname -s)" in
            Linux*)  RID="linux-x64" ;;
            Darwin*) RID="osx-x64" ;;
            MINGW*|CYGWIN*|MSYS*) RID="win-x64" ;;
            *) echo "❌ Unknown platform. Use: linux, windows, osx, or all"; exit 1 ;;
        esac
        build_platform "$RID"
        ;;
    "linux"|"windows"|"osx")
        build_platform "${RIDS[$PLATFORM]}"
        ;;
    "all")
        for rid in "${RIDS[@]}"; do
            build_platform "$rid"
        done
        ;;
    *)
        echo "❌ Unknown platform: $PLATFORM"
        echo "Usage: $0 [linux|windows|osx|all|current] [debug|release]"
        exit 1
        ;;
esac

echo ""
echo "🎉 Build complete! Output in: $OUTPUT_DIR"
echo ""
echo "To run locally:"
echo "  cd $OUTPUT_DIR/Pelican-Keeper-*"
echo "  ./\"Pelican Keeper\""
