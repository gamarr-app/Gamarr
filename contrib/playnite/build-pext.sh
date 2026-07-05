#!/usr/bin/env bash
# Builds the Gamarr Playnite library plugin and packages it as a .pext
# (a plain zip: plugin dll + dependencies + extension.yaml at the root).
#
# Usage: ./build-pext.sh [output-dir]   (default: ./dist)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/GamarrLibrary"
OUT_DIR="${1:-$SCRIPT_DIR/dist}"

VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$PROJECT_DIR/GamarrLibrary.csproj")"
PEXT="$OUT_DIR/GamarrLibrary_${VERSION}.pext"

echo "Building GamarrLibrary $VERSION..."
dotnet build "$PROJECT_DIR/GamarrLibrary.csproj" -c Release

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT

BIN="$PROJECT_DIR/bin/Release"
cp "$BIN/GamarrLibrary.dll" "$STAGE/"
cp "$BIN/Newtonsoft.Json.dll" "$STAGE/"
cp "$BIN/extension.yaml" "$STAGE/"
# Playnite.SDK.dll must NOT be bundled — Playnite provides it at runtime.

mkdir -p "$OUT_DIR"
rm -f "$PEXT"
(cd "$STAGE" && zip -q -r "$PEXT" .)

echo "Packaged: $PEXT"
unzip -l "$PEXT"
