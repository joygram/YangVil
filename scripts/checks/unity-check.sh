#!/usr/bin/env bash
set -u

MODE="${1:-index}"
ROOT="$(git rev-parse --show-toplevel)"
CHECK_DIR="${CATVILLAGE_CHECK_DIR:-$(dirname "$ROOT")/CatVillage-check}"
if [ -n "${UNITY_EXE:-}" ]; then UNITY="$UNITY_EXE"
elif [ -x "$HOME/Unity/Hub/Editor/6000.4.7f1/Editor/Unity" ]; then UNITY="$HOME/Unity/Hub/Editor/6000.4.7f1/Editor/Unity"
else UNITY="/c/Program Files/Unity/Hub/Editor/6000.4.7f1/Editor/Unity.exe"; fi
EXTRA=${UNITY_EXTRA_ARGS:-}

case "$MODE" in
  index) TREE="$(git write-tree)" ;;
  *) TREE="$MODE" ;;
esac

echo "[unity-check] 대상 트리 $TREE → $CHECK_DIR"
mkdir -p "$CHECK_DIR"
rm -rf "$CHECK_DIR/Assets" "$CHECK_DIR/Packages" "$CHECK_DIR/ProjectSettings"
git archive "$TREE" Assets Packages ProjectSettings | tar -x -C "$CHECK_DIR" || { echo "[unity-check] 트리 추출 실패"; exit 1; }

echo "[unity-check] 1/2 컴파일 확인"
for attempt in 1 2; do
  compile="$("$UNITY" -batchmode -quit $EXTRA -projectPath "$CHECK_DIR" -buildTarget WebGL -logFile - 2>&1)"
  code=$?
  errors="$(printf '%s\n' "$compile" | grep -E 'error CS|invalid parameters' | sort -u)"
  [ -n "$errors" ] && break
  [ $code -eq 0 ] && break
  echo "[unity-check] 종료 코드 $code, 컴파일 오류 없음 → 원인:"
  printf '%s\n' "$compile" | grep -iE 'another Unity|fatal|abort|licens' | grep -v 'abort_threads' | tail -5
  [ $attempt -eq 1 ] && { echo "[unity-check] 10초 후 재시도"; sleep 10; }
done
if [ $code -ne 0 ] || [ -n "$errors" ]; then
  printf '%s\n' "$errors"
  echo "[unity-check] ✘ 컴파일 실패 (종료 코드 $code)"
  exit 1
fi
echo "[unity-check] ✔ 컴파일 0건"

METHOD="CatVillage.EditorTools.PlaySmokeCheck.Run"
[ "${CATVILLAGE_BUILD_WEBGL:-0}" = "1" ] && METHOD="CatVillage.EditorTools.PlaySmokeCheck.RunThenBuildWebGL"

echo "[unity-check] 2/2 헤드리스 Play 점검 ($METHOD)"
smoke="$("$UNITY" -batchmode $EXTRA -projectPath "$CHECK_DIR" -buildTarget WebGL -executeMethod "$METHOD" -logFile - 2>&1)"
code=$?
printf '%s\n' "$smoke" | grep -E '^PlaySmoke'
if [ $code -ne 0 ] || ! printf '%s\n' "$smoke" | grep -q 'PlaySmoke: 결과 오류 0건'; then
  echo "[unity-check] ✘ Play 점검 실패 (종료 코드 $code)"
  exit 1
fi
echo "[unity-check] ✔ Play 점검 통과"
