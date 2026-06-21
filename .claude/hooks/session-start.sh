#!/bin/bash
# AutoGallery SaaS - SessionStart hook
# Claude Code on the web oturumlarinda .NET 8 SDK + bagimliliklari kurar,
# boylece "dotnet build" / "dotnet test" ve frontend tip kontrolu calisabilir.
set -euo pipefail

# Yalnizca uzak (web) ortamda calis; yerel makineyi etkileme.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

PROJECT_DIR="${CLAUDE_PROJECT_DIR:-$(pwd)}"

SUDO=""
[ "$(id -u)" != "0" ] && SUDO="sudo"

# 1) .NET 8 SDK
# Not: Bu agda Microsoft indirme host'lari (dot.net / builds.dotnet...) engelli
# oldugundan SDK, Ubuntu deposundaki dotnet-sdk-8.0 paketinden kurulur.
# (global.json bu nedenle 8.0 serisini latestFeature ile kabul edecek sekilde ayarlidir.)
if ! command -v dotnet >/dev/null 2>&1; then
  echo "[session-start] .NET SDK kuruluyor (apt)..."
  export DEBIAN_FRONTEND=noninteractive
  # PPA kaynaklarindaki imza/403 hatalari kurulumu durdurmamali.
  $SUDO apt-get update -qq || true
  $SUDO apt-get install -y -qq dotnet-sdk-8.0
fi
echo "[session-start] dotnet $(dotnet --version)"

# 2) NuGet paketlerini geri yukle (container onbellegine alir).
echo "[session-start] dotnet restore..."
dotnet restore "$PROJECT_DIR/src/Api/AutoGallerySaaS.Api.csproj" --verbosity quiet
dotnet restore "$PROJECT_DIR/tests/UnitTests/AutoGallerySaaS.UnitTests.csproj" --verbosity quiet
dotnet restore "$PROJECT_DIR/tests/IntegrationTests/AutoGallerySaaS.IntegrationTests.csproj" --verbosity quiet

# 3) Frontend bagimliliklari.
if [ -f "$PROJECT_DIR/frontend/package.json" ]; then
  echo "[session-start] npm install (frontend)..."
  ( cd "$PROJECT_DIR/frontend" && npm install --no-audit --no-fund )
fi

echo "[session-start] Hazir."
