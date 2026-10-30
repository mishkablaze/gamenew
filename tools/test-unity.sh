#!/usr/bin/env bash
# Run Unity tests in batchmode.
# Works on macOS and in Git Bash on Windows.
# Usage: tools/test-unity.sh [editmode|playmode] [extra Unity args]
# The project in THIS folder must not be open in the Unity Editor (Unity locks it).
# Override the editor path with UNITY_PATH=/path/to/Unity.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MODE="${1:-editmode}"
shift || true

case "$MODE" in
  editmode) PLATFORM="EditMode" ;;
  playmode) PLATFORM="PlayMode" ;;
  *) echo "usage: $0 [editmode|playmode] [extra Unity args]" >&2; exit 64 ;;
esac

VERSION_FILE="$ROOT/ProjectSettings/ProjectVersion.txt"
if [[ ! -f "$VERSION_FILE" ]]; then
  echo "error: $VERSION_FILE not found — is this the Unity project root?" >&2
  exit 66
fi
VERSION="$(sed -n 's/^m_EditorVersion: *//p' "$VERSION_FILE" | tr -d '\r')"

if [[ -z "${UNITY_PATH:-}" ]]; then
  case "$(uname -s)" in
    Darwin) UNITY_PATH="/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity" ;;
    MINGW*|MSYS*|CYGWIN*) UNITY_PATH="/c/Program Files/Unity/Hub/Editor/$VERSION/Editor/Unity.exe" ;;
    *) echo "error: unsupported OS $(uname -s); set UNITY_PATH" >&2; exit 69 ;;
  esac
fi
if [[ ! -x "$UNITY_PATH" ]]; then
  echo "error: Unity $VERSION not found at: $UNITY_PATH (install it via Unity Hub or set UNITY_PATH)" >&2
  exit 69
fi

mkdir -p "$ROOT/Logs"
RESULTS="$ROOT/Logs/${MODE}-results.xml"
LOG="$ROOT/Logs/${MODE}.log"
rm -f "$RESULTS"

echo "== Unity $VERSION · $PLATFORM tests =="
set +e
"$UNITY_PATH" -batchmode -nographics \
  -projectPath "$ROOT" \
  -runTests -testPlatform "$PLATFORM" \
  -testResults "$RESULTS" \
  -logFile "$LOG" "$@"
CODE=$?
set -e

if [[ -f "$RESULTS" ]]; then
  SUMMARY="$(grep -o '<test-run[^>]*>' "$RESULTS" | head -1)"
  attr() { echo "$SUMMARY" | sed -n "s/.* $1=\"\\([^\"]*\\)\".*/\\1/p"; }
  echo "total=$(attr total) passed=$(attr passed) failed=$(attr failed) skipped=$(attr skipped) result=$(attr result)"
  if [[ "$(attr failed)" != "0" ]]; then
    echo "-- failed tests --"
    grep -o '<test-case [^>]*result="Failed"[^>]*>' "$RESULTS" \
      | sed -n 's/.* fullname="\([^"]*\)".*/  \1/p'
  fi
else
  echo "no results file — see $LOG (compile errors or the project is open in the Editor?)" >&2
fi

# Unity exit codes: 0 = all passed, 2 = some tests failed, 3 = run error
exit "$CODE"
