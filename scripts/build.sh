#!/usr/bin/env bash
set -euo pipefail

: "${VALHEIM_MANAGED_DIR:?Set VALHEIM_MANAGED_DIR to Valheim/valheim_Data/Managed}"
: "${BEPINEX_CORE_DIR:?Set BEPINEX_CORE_DIR to BepInEx/core}"
: "${JOTUNN_DIR:?Set JOTUNN_DIR to the directory containing Jotunn.dll}"

dotnet build src/RagnavikGameplay.csproj --configuration Release \
  -p:ValheimManagedDir="$VALHEIM_MANAGED_DIR" \
  -p:BepInExCoreDir="$BEPINEX_CORE_DIR" \
  -p:JotunnDir="$JOTUNN_DIR"
dotnet run --project tests/RagnavikGameplay.Tests.csproj --configuration Release

