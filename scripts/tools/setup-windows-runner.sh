#!/usr/bin/env bash
set -euo pipefail

REPO="joygram/YangVil"
RUNNER_DIR="${RUNNER_DIR:-/d/actions-runner}"
UNITY_EXE="${UNITY_EXE:-C:/Program Files/Unity/Hub/Editor/6000.4.7f1/Editor/Unity.exe}"

command -v gh >/dev/null || { echo "gh CLI 필요"; exit 1; }
[ -f "$UNITY_EXE" ] || { echo "Unity 없음: $UNITY_EXE"; exit 1; }

echo "[1/4] 러너 등록 토큰"
TOKEN="$(gh api -X POST "repos/$REPO/actions/runners/registration-token" --jq .token)"

echo "[2/4] 러너 내려받기 → $RUNNER_DIR"
mkdir -p "$RUNNER_DIR"
cd "$RUNNER_DIR"
if [ ! -f ./config.cmd ]; then
  tag="$(gh api repos/actions/runner/releases/latest --jq .tag_name)"
  ver="${tag#v}"
  curl -fsSL -o runner.zip "https://github.com/actions/runner/releases/download/$tag/actions-runner-win-x64-$ver.zip"
  unzip -q runner.zip
  rm runner.zip
fi

echo "[3/4] 러너 설정 (라벨 unity, windows)"
./config.cmd --unattended --url "https://github.com/$REPO" --token "$TOKEN" --name "$(hostname)-unity" --labels unity,windows --replace
printf 'UNITY_EXE=%s\n' "$UNITY_EXE" > .env

echo "[4/4] 실행"
echo "현재 로그인 사용자로 실행해야 Unity 라이선스·wrangler 로그인을 그대로 씀"
echo "→ 이 창에서: cd $RUNNER_DIR && ./run.cmd   (PC 켜져 있는 동안 동작)"
echo "→ 로그인 시 자동 시작: 시작프로그램 폴더에 run.cmd 바로가기 추가"
