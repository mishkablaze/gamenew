#!/usr/bin/env bash
# Fast Raidline.Core tests via dotnet (no Unity needed).
# Works on macOS and in Git Bash on Windows.
# Usage: tools/test-core.sh [extra dotnet test args]
#   e.g. tools/test-core.sh --filter "FullyQualifiedName~Inventory"
#   SDK newer than net8.0 only:  tools/test-core.sh -p:CoreTestsTfm=net9.0
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJ="$ROOT/tools/CoreTests/CoreTests.csproj"

if ! command -v dotnet >/dev/null 2>&1; then
  if [[ -x "$HOME/.dotnet/dotnet" ]]; then
    export PATH="$HOME/.dotnet:$PATH"
  else
    echo "error: dotnet SDK not found (install .NET 8 SDK or put it on PATH)" >&2
    exit 127
  fi
fi

echo "== Raidline Core tests (dotnet) =="
dotnet test "$PROJ" --nologo -v quiet "$@"
