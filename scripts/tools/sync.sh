#!/usr/bin/env bash
set -u

ROOT="$(git rev-parse --show-toplevel)" || exit 1
cd "$ROOT" || exit 1

AUTO_PATTERN='^(Assets/Art/UI/Fonts/.*\.asset|ProjectSettings/.*|Packages/packages-lock\.json|Assets/AddressableAssetsData/.*\.bin|Assets/Resources/PerformanceTestRun.*)$'

changed="$(git status --porcelain --untracked-files=no | cut -c4-)"
auto="$(printf '%s\n' "$changed" | grep -E "$AUTO_PATTERN" || true)"
manual="$(printf '%s\n' "$changed" | grep -vE "$AUTO_PATTERN" | grep -v '^$' || true)"

if [ -n "$auto" ]; then
  echo "[sync] 에디터 자동 변경 버림 → 원격 기준으로 받음:"
  printf '%s\n' "$auto" | sed 's/^/  /'
  printf '%s\n' "$auto" | while IFS= read -r f; do git restore --source=HEAD --staged --worktree -- "$f"; done
fi

if [ -n "$manual" ]; then
  echo "[sync] ✘ 직접 수정한 파일이 있어 자동으로 받지 않음 (커밋하거나 정리 후 다시 실행):"
  printf '%s\n' "$manual" | sed 's/^/  /'
  exit 1
fi

git fetch --quiet origin || exit 1
BRANCH="$(git rev-parse --abbrev-ref HEAD)"
if git merge-base --is-ancestor HEAD "origin/$BRANCH"; then
  git merge --ff-only --quiet "origin/$BRANCH"
elif [ "${SYNC_FOLLOW_REMOTE:-0}" = "1" ]; then
  echo "[sync] 원격과 이력이 갈라짐 → 이 폴더는 원격 추종용 → 원격 기준으로 맞춤"
  git reset --hard --quiet "origin/$BRANCH"
else
  echo "[sync] ✘ 원격과 이력이 갈라짐 (원격 이력이 다시 쓰였거나 로컬 커밋 있음):"
  git log --oneline "origin/$BRANCH..HEAD" | head -5 | sed 's/^/  로컬만: /'
  echo "  원격 기준으로 맞추려면: SYNC_FOLLOW_REMOTE=1 bash $0"
  exit 1
fi
echo "[sync] ✔ $(git rev-parse --short HEAD) 최신"
