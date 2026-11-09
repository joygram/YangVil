#!/usr/bin/env bash
set -uo pipefail

REPO="joygram/YangVil"
UNITY_VERSION="6000.4.7f1"
UNITY_CHANGESET="f3c3c4248748"
TARGET_USER="${SUDO_USER:-$USER}"
TARGET_HOME="$(getent passwd "$TARGET_USER" | cut -d: -f6)"
UNITY_EXE="$TARGET_HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity"
LICENSE_DIR="$TARGET_HOME/.local/share/unity3d/Unity/licenses"
RUNNER_DIR="${RUNNER_DIR:-$TARGET_HOME/actions-runner}"

as_user() { if [ "$(id -un)" = "$TARGET_USER" ]; then "$@"; else sudo -u "$TARGET_USER" -H "$@"; fi; }

deps() {
  echo "[deps] 기본 패키지"
  sudo apt-get update -qq
  sudo apt-get install -y git curl ca-certificates gnupg tar unzip libglu1-mesa >/dev/null
  if ! command -v node >/dev/null; then
    curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash - >/dev/null
    sudo apt-get install -y nodejs >/dev/null
  fi
  if ! command -v gh >/dev/null; then
    curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg status=none
    echo "deb [signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list >/dev/null
    sudo apt-get update -qq && sudo apt-get install -y gh >/dev/null
  fi
  echo "  node $(node -v), gh $(gh --version | head -1 | awk '{print $3}')"
}

hub() {
  echo "[hub] Unity Hub"
  if ! command -v unityhub >/dev/null; then
    curl -fsSL https://hub.unity3d.com/linux/keys/public | sudo gpg --dearmor --yes -o /usr/share/keyrings/Unity_Technologies_ApS.gpg
    echo "deb [signed-by=/usr/share/keyrings/Unity_Technologies_ApS.gpg] https://hub.unity3d.com/linux/repos/deb stable main" | sudo tee /etc/apt/sources.list.d/unityhub.list >/dev/null
    sudo apt-get update -qq && sudo apt-get install -y unityhub >/dev/null
  fi
  command -v unityhub >/dev/null && echo "  unityhub 설치됨" || { echo "  ✘ unityhub 설치 실패"; return 1; }
}

editor() {
  echo "[editor] Unity $UNITY_VERSION + WebGL 모듈"
  if [ ! -x "$UNITY_EXE" ]; then
    as_user unityhub --headless install-path --set "$TARGET_HOME/Unity/Hub/Editor" >/dev/null 2>&1
    as_user unityhub --headless install --version "$UNITY_VERSION" --changeset "$UNITY_CHANGESET" --module webgl
  elif [ ! -d "$(dirname "$UNITY_EXE")/Data/PlaybackEngines/WebGLSupport" ]; then
    as_user unityhub --headless install-modules --version "$UNITY_VERSION" --module webgl
  fi
  [ -x "$UNITY_EXE" ] && echo "  ✔ $UNITY_EXE" || { echo "  ✘ 에디터 없음"; return 1; }
  [ -d "$(dirname "$UNITY_EXE")/Data/PlaybackEngines/WebGLSupport" ] && echo "  ✔ WebGL 모듈" || { echo "  ✘ WebGL 모듈 없음"; return 1; }
}

license() {
  echo "[license] Unity 라이선스"
  if ls "$LICENSE_DIR"/*.xml >/dev/null 2>&1; then
    echo "  ✔ 라이선스 파일 있음: $(ls "$LICENSE_DIR")"
    return 0
  fi
  echo "  ✘ 라이선스 없음 → 원격 데스크톱 터미널에서 'unityhub' 실행 → 로그인 → Preferences > Licenses > Add > Personal"
  echo "    끝나면: bash $0 license"
  return 1
}

check() {
  echo "[check] 헤드리스 실행 확인"
  out="$(as_user "$UNITY_EXE" -batchmode -quit -nographics -logFile - 2>&1)"
  code=$?
  printf '%s\n' "$out" | grep -iE 'licens|entitlement' | grep -viE 'debug' | tail -3 | sed 's/^/  /'
  [ $code -eq 0 ] && echo "  ✔ Unity 배치 실행 정상" || { echo "  ✘ 종료 코드 $code"; return 1; }
}

register() {
  echo "[runner] GitHub Actions 러너"
  local token="${1:-}"
  if [ -z "$token" ]; then
    as_user gh auth status >/dev/null 2>&1 || { echo "  먼저: gh auth login  (또는 토큰을 인자로)"; return 1; }
    token="$(as_user gh api -X POST "repos/$REPO/actions/runners/registration-token" --jq .token)" || return 1
  fi
  as_user mkdir -p "$RUNNER_DIR"
  cd "$RUNNER_DIR" || return 1
  if [ ! -f ./config.sh ]; then
    tag="$(curl -fsSL https://api.github.com/repos/actions/runner/releases/latest | grep -m1 '"tag_name"' | cut -d'"' -f4)"
    as_user curl -fsSL -o runner.tar.gz "https://github.com/actions/runner/releases/download/$tag/actions-runner-linux-x64-${tag#v}.tar.gz"
    as_user tar xzf runner.tar.gz && rm -f runner.tar.gz
  fi
  as_user ./config.sh --unattended --url "https://github.com/$REPO" --token "$token" --name "$(hostname)-unity" --labels unity,linux --replace || return 1
  printf 'UNITY_EXE=%s\nUNITY_EXTRA_ARGS=-nographics\n' "$UNITY_EXE" | as_user tee .env >/dev/null
  sudo ./svc.sh install "$TARGET_USER" >/dev/null 2>&1 || true
  sudo ./svc.sh start
  sudo ./svc.sh status | grep -m1 -E 'Active:' | sed 's/^ */  /'
}

case "${1:-all}" in
  deps) deps ;;
  hub) hub ;;
  editor) editor ;;
  license) license ;;
  check) check ;;
  register) shift; register "${1:-}" ;;
  install) deps && hub && editor && license && check ;;
  all) deps && hub && editor && license && check && register "${2:-}" ;;
  *) echo "사용법: bash $0 [install|license|check|register [토큰]|all]"; exit 1 ;;
esac
