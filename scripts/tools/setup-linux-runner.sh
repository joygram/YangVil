#!/usr/bin/env bash
set -euo pipefail

REPO_URL="https://github.com/joygram/YangVil"
UNITY_VERSION="6000.4.7f1"
UNITY_CHANGESET="f3c3c4248748"
RUNNER_DIR="${RUNNER_DIR:-$HOME/actions-runner}"
TOKEN="${1:-}"

if [ -z "$TOKEN" ]; then
  echo "사용법: bash setup-linux-runner.sh <러너 등록 토큰>"
  echo "토큰: gh api -X POST repos/joygram/YangVil/actions/runners/registration-token --jq .token"
  exit 1
fi

echo "[1/5] 기본 패키지 (git·curl·node·libs)"
sudo apt-get update
sudo apt-get install -y git curl ca-certificates gnupg tar libgconf-2-4 libglu1-mesa libasound2t64 || \
  sudo apt-get install -y git curl ca-certificates gnupg tar libglu1-mesa libasound2
if ! command -v node >/dev/null; then
  curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
  sudo apt-get install -y nodejs
fi

echo "[2/5] Unity Hub"
if ! command -v unityhub >/dev/null; then
  curl -fsSL https://hub.unity3d.com/linux/keys/public | sudo gpg --dearmor -o /usr/share/keyrings/Unity_Technologies_ApS.gpg
  echo "deb [signed-by=/usr/share/keyrings/Unity_Technologies_ApS.gpg] https://hub.unity3d.com/linux/repos/deb stable main" | \
    sudo tee /etc/apt/sources.list.d/unityhub.list >/dev/null
  sudo apt-get update
  sudo apt-get install -y unityhub
fi

echo "[3/5] Unity $UNITY_VERSION + WebGL 모듈"
UNITY_EXE="$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity"
if [ ! -x "$UNITY_EXE" ]; then
  unityhub --headless install --version "$UNITY_VERSION" --changeset "$UNITY_CHANGESET" --module webgl
fi
[ -x "$UNITY_EXE" ] || { echo "Unity 설치 실패: $UNITY_EXE 없음"; exit 1; }

echo "[3b/5] Unity Personal 라이선스 (터미널)"
license_ok() { "$UNITY_EXE" -batchmode -quit -nographics -logFile - 2>&1 | grep -qiE 'entitlement|license.*(valid|activated)' && \
  ! "$UNITY_EXE" -batchmode -quit -nographics -logFile - 2>&1 | grep -qiE 'no valid unity editor license|license is not active'; }
if license_ok; then
  echo "라이선스 이미 활성화됨"
elif [ -n "${UNITY_USERNAME:-}" ] && [ -n "${UNITY_PASSWORD:-}" ]; then
  npx --yes @rage-against-the-pixel/unity-cli activate-license --license personal || true
  license_ok && echo "라이선스 활성화 성공" || echo "라이선스 활성화 실패 → 아래 수동 단계 1) 진행"
else
  echo "UNITY_USERNAME·UNITY_PASSWORD 환경변수 없음 → 라이선스 단계 건너뜀"
fi

echo "[4/5] GitHub Actions 러너"
mkdir -p "$RUNNER_DIR"
cd "$RUNNER_DIR"
if [ ! -f ./config.sh ]; then
  tag="$(curl -fsSL https://api.github.com/repos/actions/runner/releases/latest | grep -m1 '"tag_name"' | cut -d'"' -f4)"
  ver="${tag#v}"
  curl -fsSL -o runner.tar.gz "https://github.com/actions/runner/releases/download/$tag/actions-runner-linux-x64-$ver.tar.gz"
  tar xzf runner.tar.gz
  rm runner.tar.gz
fi
./config.sh --unattended --url "$REPO_URL" --token "$TOKEN" --name "$(hostname)-unity" --labels unity,linux --replace
printf 'UNITY_EXE=%s\nUNITY_EXTRA_ARGS=-nographics\n' "$UNITY_EXE" > .env

echo "[5/5] 러너 서비스 등록·시작"
sudo ./svc.sh install "$USER"
sudo ./svc.sh start

echo ""
echo "완료. 남은 수동 단계 (한 번만):"
echo "    확인: $UNITY_EXE -batchmode -quit -nographics -logFile - | grep -i licens"
echo " 2) GitHub 시크릿 CLOUDFLARE_API_TOKEN 등록 (Workers 편집 권한)"
