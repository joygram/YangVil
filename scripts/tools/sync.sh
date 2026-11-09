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

git pull --ff-only --quiet && echo "[sync] ✔ $(git rev-parse --short HEAD) 최신"
