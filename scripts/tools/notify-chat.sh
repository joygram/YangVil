#!/usr/bin/env bash
set -u

KIND="${1:-done}"
[ -z "${GOOGLE_CHAT_WEBHOOK:-}" ] && { echo "[notify] GOOGLE_CHAT_WEBHOOK 없음 → 알림 생략"; exit 0; }

SHA="${GITHUB_SHA:-$(git rev-parse HEAD)}"
SHORT="${SHA:0:7}"
URL="https://yangvil.deuklab.workers.dev"
RUN="${RUN_URL:-}"
ACTOR="${GITHUB_ACTOR:-}"
CHANGES="$(git log --format='• %s' -n "${CHANGE_COUNT:-5}" "$SHA" 2>/dev/null)"

case "$KIND" in
  start) TEXT="🛠 *냥빌 빌드 시작* \`$SHORT\` ($ACTOR)
$CHANGES
$RUN" ;;
  done) TEXT="✅ *냥빌 새 버전 배포 완료* \`$SHORT\`${ELAPSED:+ · ${ELAPSED}}
▶ $URL
$CHANGES
(열려 있는 게임은 자동으로 새 버전 안내가 떠요)" ;;
  *) TEXT="❌ *냥빌 빌드·배포 실패* \`$SHORT\` ($ACTOR)
$CHANGES
로그: $RUN" ;;
esac

node -e 'process.stdout.write(JSON.stringify({text:process.argv[1]}))' "$TEXT" |
  curl -sS -o /dev/null -w '[notify] HTTP %{http_code}\n' -X POST -H 'Content-Type: application/json; charset=UTF-8' -d @- "$GOOGLE_CHAT_WEBHOOK"
