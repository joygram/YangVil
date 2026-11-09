#!/usr/bin/env bash
set -u

if [ $# -lt 2 ]; then
  echo "사용법: bash regen-content.sh <ExecuteMethod> <원본으로 가져올 경로>..."
  echo "예: bash regen-content.sh CatVillage.EditorTools.GameDataBuilder.Build Assets/Data Assets/Resources/GameConfig.asset"
  exit 1
fi

METHOD="$1"; shift
ROOT="$(git rev-parse --show-toplevel)"
CHECK_DIR="${CATVILLAGE_CHECK_DIR:-$(dirname "$ROOT")/CatVillage-check}"
source "$ROOT/scripts/checks/unity-path.sh"

TREE="$(git -C "$ROOT" write-tree)"
mkdir -p "$CHECK_DIR"
rm -rf "$CHECK_DIR/Assets" "$CHECK_DIR/Packages" "$CHECK_DIR/ProjectSettings"
git -C "$ROOT" archive "$TREE" Assets Packages ProjectSettings | tar -x -C "$CHECK_DIR" || exit 1
if [ -n "${UNITY_EXE:-}" ]; then UNITY="$UNITY_EXE"; else resolve_unity "$CHECK_DIR" || exit 1; fi

for attempt in 1 2; do
  out="$("$UNITY" -batchmode -quit ${UNITY_EXTRA_ARGS:-} -projectPath "$CHECK_DIR" -buildTarget WebGL -executeMethod "$METHOD" -logFile - 2>&1)"
  code=$?
  errors="$(printf '%s\n' "$out" | grep -E 'error CS|^[A-Za-z.]*Exception:' | sort -u)"
  [ $code -eq 0 ] && [ -z "$errors" ] && break
  [ $attempt -eq 1 ] && [ -z "$errors" ] && { echo "[regen] 종료 코드 $code → 10초 후 재시도"; sleep 10; continue; }
  printf '%s\n' "$errors"
  echo "[regen] ✘ $METHOD 실패 (종료 코드 $code)"
  exit 1
done
printf '%s\n' "$out" | grep -E 'Builder:|Setup:' | grep -v '(at ' | sort -u | sed 's/^/  /'

for p in "$@"; do
  if [ -d "$CHECK_DIR/$p" ]; then
    mkdir -p "$ROOT/$p"
    cp -r "$CHECK_DIR/$p/." "$ROOT/$p/"
  else
    cp "$CHECK_DIR/$p" "$ROOT/$p"
  fi
  echo "[regen] ✔ $p → 원본"
done
