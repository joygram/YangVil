#!/usr/bin/env bash
set -u

ROOT="$(git rev-parse --show-toplevel)"
CHECK_DIR="${CATVILLAGE_CHECK_DIR:-$(dirname "$ROOT")/CatVillage-check}"

DIRTY="$(git -C "$ROOT" status --porcelain -- Assets Packages ProjectSettings)"
if [ -n "$DIRTY" ]; then
  echo "[deploy-web] ⚠ 커밋 안 된 Unity 변경은 배포에 포함 안 됨 (HEAD $(git -C "$ROOT" rev-parse --short HEAD) 기준):"
  printf '%s\n' "$DIRTY" | sed 's/^/  /'
fi

CATVILLAGE_BUILD_WEBGL=1 bash "$ROOT/scripts/checks/unity-check.sh" HEAD || { echo "[deploy-web] 검증 실패 → 배포 중단"; exit 1; }

cp "$ROOT/wrangler.jsonc" "$CHECK_DIR/wrangler.jsonc"
(cd "$CHECK_DIR" && npx --yes wrangler deploy) || { echo "[deploy-web] wrangler 배포 실패"; exit 1; }
echo "[deploy-web] ✔ 배포 완료 → https://yangvil.deuklab.workers.dev"
