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

# ── Inject version (release builds only) ─────────────────────────────────────────
# Only runs for --release. Requires commits to be pushed first so the Release
# Please PR exists with the target version. Falls back to the manifest if no PR
# is open (e.g., a hotfix build outside the normal release cycle).
if [[ "$CONFIGURATION" == "Release" ]]; then
    MANIFEST="$SCRIPT_DIR/.release-please-manifest.json"
    GH_REPO=$(git remote get-url origin 2>/dev/null | sed 's|.*github.com[:/]\(.*\)\.git|\1|;s|.*github.com[:/]\(.*\)|\1|')
    RELEASE_PR_VERSION=$(gh pr list --repo "${GH_REPO}" --state open --json title \
        --jq '.[].title | select(test("^chore\\(m(ain|aster)\\): release ")) | split(" ")[-1]' \
        2>/dev/null | head -1)
    if [[ -n "$RELEASE_PR_VERSION" ]]; then
        MOD_VERSION="$RELEASE_PR_VERSION"
        echo "Version: $MOD_VERSION (from open Release Please PR)"
    elif [[ -f "$MANIFEST" ]]; then
        MOD_VERSION=$(python3 -c "import json; print(json.load(open('$MANIFEST'))['.'])")
        echo "Version: $MOD_VERSION (from manifest — no open release PR found)"
    fi
    if [[ -n "${MOD_VERSION:-}" ]]; then
        ASSEMBLY_INFO="$SCRIPT_DIR/Properties/AssemblyInfo.cs"
        sed -i "s/\[assembly: AssemblyVersion(\"[^\"]*\")\]/[assembly: AssemblyVersion(\"$MOD_VERSION.0\")]/" "$ASSEMBLY_INFO"
    fi
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
if [[ -d "$SCRIPT_DIR/Resources" ]]; then
    cp -r "$SCRIPT_DIR/Resources" "$DIST/Resources"
fi
echo ""
echo "Staged to: $DIST"
ls -lh "$DIST"

# ── Deploy to game ────────────────────────────────────────────────────────────────
echo ""
echo "Copying to $MODS_DIR ..."
mkdir -p "$MODS_DIR"
cp "$DIST/$MOD_NAME.dll" "$MODS_DIR/"
cp -r "$DIST/Locale" "$MODS_DIR/"
if [[ -d "$DIST/Resources" ]]; then
    rm -rf "$MODS_DIR/Resources"
    cp -r "$DIST/Resources" "$MODS_DIR/"
fi
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
