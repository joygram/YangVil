#!/usr/bin/env bash
set -u

ROOT="$(git rev-parse --show-toplevel)"
CHECK_DIR="${YANGVIL_CHECK_DIR:-$(dirname "$ROOT")/YangVil-check}"

DIRTY="$(git -C "$ROOT" status --porcelain -- Assets Packages ProjectSettings)"
if [ -n "$DIRTY" ]; then
  echo "[deploy-web] ⚠ 커밋 안 된 Unity 변경은 배포에 포함 안 됨 (HEAD $(git -C "$ROOT" rev-parse --short HEAD) 기준):"
  printf '%s\n' "$DIRTY" | sed 's/^/  /'
fi

YANGVIL_BUILD_ONLY=1 bash "$ROOT/scripts/checks/unity-check.sh" HEAD || { echo "[deploy-web] 빌드 실패 → 배포 중단"; exit 1; }

NODE_MAJOR="$(node -v 2>/dev/null | sed 's/^v\([0-9]*\).*/\1/')"
if [ -z "$NODE_MAJOR" ] || [ "$NODE_MAJOR" -lt 22 ]; then
  echo "[deploy-web] ✘ Node.js ${NODE_MAJOR:-없음} → wrangler는 22 이상 필요 (리눅스: setup-linux-runner.sh node → 러너 재등록)"
  exit 1
fi

cp "$ROOT/wrangler.jsonc" "$CHECK_DIR/wrangler.jsonc"
(cd "$CHECK_DIR" && npx --yes wrangler deploy) || { echo "[deploy-web] wrangler 배포 실패"; exit 1; }
echo "[deploy-web] ✔ 배포 완료 → https://yangvil.deuklab.workers.dev"
