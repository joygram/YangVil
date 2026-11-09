#!/usr/bin/env bash
set -u

ROOT="$(git rev-parse --show-toplevel)"
CHECK_DIR="${CATVILLAGE_CHECK_DIR:-$(dirname "$ROOT")/CatVillage-check}"
UNITY="${UNITY_EXE:-/c/Program Files/Unity/Hub/Editor/6000.4.7f1/Editor/Unity.exe}"
TREE="$(git -C "$ROOT" write-tree)"

mkdir -p "$CHECK_DIR"
rm -rf "$CHECK_DIR/Assets" "$CHECK_DIR/Packages" "$CHECK_DIR/ProjectSettings"
git -C "$ROOT" archive "$TREE" Assets Packages ProjectSettings | tar -x -C "$CHECK_DIR" || exit 1

for attempt in 1 2; do
  out="$("$UNITY" -batchmode -quit -projectPath "$CHECK_DIR" -buildTarget WebGL -executeMethod CatVillage.EditorTools.UiPrefabBuilder.Build -logFile - 2>&1)"
  code=$?
  printf '%s\n' "$out" | grep -q 'UiPrefabBuilder: 화면' && break
  [ $attempt -eq 1 ] && { echo "[regen-ui] 종료 코드 $code → 10초 후 재시도"; sleep 10; }
done
printf '%s\n' "$out" | grep -E 'error CS|UiPrefabBuilder:' | sort -u
if [ $code -ne 0 ] || ! printf '%s\n' "$out" | grep -q 'UiPrefabBuilder: 화면'; then
  echo "[regen-ui] ✘ 프리팹 생성 실패 (종료 코드 $code)"
  exit 1
fi

cp -r "$CHECK_DIR/Assets/Prefabs/UI/." "$ROOT/Assets/Prefabs/UI/"
cp "$CHECK_DIR/Assets/Resources/GameConfig.asset" "$ROOT/Assets/Resources/GameConfig.asset"
echo "[regen-ui] ✔ UI 프리팹 갱신 → 원본에 복사 (디자이너가 손본 프리팹은 덮어씀)"
