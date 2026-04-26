#!/usr/bin/env bash
# deploy.sh — Build IPT Essentials, stage files, and copy to the game's Mods folder.
#
# Configuration is read from .env in the repo root (copy .env.example to .env).
#
# Usage:
#   ./deploy.sh           → Debug build
#   ./deploy.sh --release → Release build

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [[ -f "$SCRIPT_DIR/.env" ]]; then
    while IFS='=' read -r _key _value; do
        [[ "$_key" =~ ^[[:space:]]*# ]] && continue
        [[ -z "$_key" ]]               && continue
        _value="${_value%\"}"  ; _value="${_value#\"}"
        _value="${_value%\'}"  ; _value="${_value#\'}"
        export "$_key=$_value"
    done < "$SCRIPT_DIR/.env"
    unset _key _value
fi

CONFIGURATION="Debug"
if [[ "${1:-}" == "--release" ]]; then
    CONFIGURATION="Release"
fi

MOD_NAME="IPTEssentials"
BUILD_OUT="$SCRIPT_DIR/bin/$CONFIGURATION"
DIST="$SCRIPT_DIR/dist/$MOD_NAME"

GAME_MOUNT="${CITIES_GAME_MOUNT:-/mnt/cities_skylines}"
DATA_MOUNT="${CITIES_DATA_MOUNT:-/mnt/cities_skylines_data}"
MODS_DIR="$DATA_MOUNT/Addons/Mods/$MOD_NAME"
LOG_FILE="$DATA_MOUNT/output_log.txt"

# ── Mount check ──────────────────────────────────────────────────────────────────
if ! mountpoint -q "$DATA_MOUNT"; then
    echo "ERROR: $DATA_MOUNT is not mounted."
    echo "Run ./mount-cities.sh first."
    exit 1
fi

# ── Build ────────────────────────────────────────────────────────────────────────
echo "Building ($CONFIGURATION)..."
cd "$SCRIPT_DIR"
xbuild ImprovedPublicTransport.sln /p:Configuration="$CONFIGURATION" /p:ReferencePath="$GAME_MOUNT/Cities_Data/Managed/" /nologo /verbosity:quiet
echo "Build succeeded."

# ── Stage to dist/ ───────────────────────────────────────────────────────────────
rm -rf "$DIST"
mkdir -p "$DIST"
cp "$BUILD_OUT/ImprovedPublicTransport2.dll" "$DIST/$MOD_NAME.dll"
cp -r "$SCRIPT_DIR/Locale" "$DIST/Locale"
echo ""
echo "Staged to: $DIST"
ls -lh "$DIST"

# ── Deploy to game ────────────────────────────────────────────────────────────────
echo ""
echo "Copying to $MODS_DIR ..."
mkdir -p "$MODS_DIR"
cp "$DIST/$MOD_NAME.dll" "$MODS_DIR/"
cp -r "$DIST/Locale" "$MODS_DIR/"
echo "Done. Files in game Mods folder:"
ls -lh "$MODS_DIR"

# ── Show log tail ─────────────────────────────────────────────────────────────────
echo ""
if [[ -f "$LOG_FILE" ]]; then
    echo "════════════════════════  output_log.txt (last 60 lines)  ════════════════════════"
    tail -n 60 "$LOG_FILE"
    echo "══════════════════════════════════════════════════════════════════════════════════"
else
    echo "Log not found at $LOG_FILE"
fi
