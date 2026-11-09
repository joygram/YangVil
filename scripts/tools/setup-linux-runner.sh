#!/usr/bin/env bash
set -uo pipefail

REPO="joygram/YangVil"
REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
source "$REPO_ROOT/scripts/checks/unity-path.sh"
UNITY_VERSION="$(unity_version "$REPO_ROOT")"
UNITY_CHANGESET="$(unity_changeset "$REPO_ROOT")"
[ -n "$UNITY_VERSION" ] && [ -n "$UNITY_CHANGESET" ] || { echo "ProjectSettings/ProjectVersion.txt 읽기 실패"; exit 1; }
TARGET_USER="${SUDO_USER:-$USER}"
TARGET_HOME="$(getent passwd "$TARGET_USER" | cut -d: -f6)"
find_unity() {
  local p
  p="$(UNITY_EDITOR_ROOT="$TARGET_HOME/Unity/Hub/Editor" HOME="$TARGET_HOME" unity_exe "$UNITY_VERSION")" && { echo "$p"; return; }
  if command -v unityhub >/dev/null; then
    p="$(as_user unityhub --headless editors --installed 2>/dev/null | grep "$UNITY_VERSION" | grep -o '/[^ ]*' | head -1)"
    [ -n "$p" ] && [ -d "$p" ] && p="$p/Editor/Unity"
    [ -n "$p" ] && [ -x "$p" ] && { echo "$p"; return; }
  fi
  find "$TARGET_HOME" /opt -path "*$UNITY_VERSION*/Editor/Unity" -type f -perm -u+x 2>/dev/null | head -1
}
LICENSE_DIRS=("$TARGET_HOME/.local/share/unity3d/licenses" "$TARGET_HOME/.local/share/unity3d/Unity/licenses" "$TARGET_HOME/.local/share/unity3d/Unity")
RUNNER_DIR="${RUNNER_DIR:-$TARGET_HOME/actions-runner}"

as_user() { if [ "$(id -un)" = "$TARGET_USER" ]; then "$@"; else sudo -u "$TARGET_USER" -H "$@"; fi; }

UNITY_EXE="$(find_unity)"
[ -n "$UNITY_EXE" ] || UNITY_EXE="$TARGET_HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity"

need_unity() {
  [ -x "$UNITY_EXE" ] && return 0
  echo "  ✘ Unity $UNITY_VERSION 실행 파일 없음 ($UNITY_EXE)"
  echo "    설치 중이면 끝난 뒤 다시 실행 / 설치: bash $0 editor"
  echo "    설치된 에디터 목록: unityhub --headless editors --installed"
  return 1
}

NODE_MIN=22
NVM_VERSION="v0.40.3"
NVM_DIR_TARGET="$TARGET_HOME/.nvm"

nvm_run() { as_user bash -c "export NVM_DIR='$NVM_DIR_TARGET'; . \"\$NVM_DIR/nvm.sh\" >/dev/null; $*"; }

ensure_node() {
  if [ ! -s "$NVM_DIR_TARGET/nvm.sh" ]; then
    echo "  nvm $NVM_VERSION 설치 ($TARGET_USER 계정)"
    as_user bash -c "curl -fsSL https://raw.githubusercontent.com/nvm-sh/nvm/$NVM_VERSION/install.sh | PROFILE=/dev/null bash" >/dev/null || { echo "  ✘ nvm 설치 실패"; return 1; }
  fi
  nvm_run "nvm ls $NODE_MIN >/dev/null 2>&1 || nvm install $NODE_MIN >/dev/null; nvm alias default $NODE_MIN >/dev/null" || { echo "  ✘ nvm install $NODE_MIN 실패"; return 1; }
  NODE_BIN="$(nvm_run "dirname \"\$(nvm which $NODE_MIN)\"")"
  [ -x "$NODE_BIN/node" ] || { echo "  ✘ Node $NODE_MIN 경로 없음"; return 1; }
  export PATH="$NODE_BIN:$PATH"
  hash -r
  echo "  ✔ node $(node -v) ($NODE_BIN, nvm)"
}

deps() {
  echo "[deps] 기본 패키지"
  sudo apt-get update -qq
  sudo apt-get install -y git curl ca-certificates gnupg tar unzip libglu1-mesa >/dev/null
  ensure_node
  if ! command -v gh >/dev/null; then
    curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg status=none
    echo "deb [signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list >/dev/null
    sudo apt-get update -qq && sudo apt-get install -y gh >/dev/null
  fi
  echo "  node $(node -v), gh $(gh --version | head -1 | awk '{print $3}')"
  git -C "$REPO_ROOT" config alias.sync '!SYNC_FOLLOW_REMOTE=1 bash scripts/tools/sync.sh'
  git -C "$REPO_ROOT" config pull.ff only
  echo "  git sync 등록 (git pull 대신 사용: 에디터 자동 변경 정리 + 원격 추종)"
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
  UNITY_EXE="$(find_unity)"
  [ -x "$UNITY_EXE" ] && echo "  ✔ $UNITY_EXE" || { echo "  ✘ 에디터 없음"; return 1; }
  [ -d "$(dirname "$UNITY_EXE")/Data/PlaybackEngines/WebGLSupport" ] && echo "  ✔ WebGL 모듈" || { echo "  ✘ WebGL 모듈 없음"; return 1; }
}

license() {
  echo "[license] Unity 라이선스"
  need_unity || return 1
  for d in "${LICENSE_DIRS[@]}"; do
    f="$(ls "$d"/*.xml "$d"/*.ulf 2>/dev/null | head -1)"
    [ -n "$f" ] && { echo "  ✔ 라이선스 파일: $f"; return 0; }
  done
  out="$(as_user "$UNITY_EXE" -batchmode -quit -nographics -logFile - 2>&1)"
  if printf '%s\n' "$out" | grep -qiE 'no valid unity editor license|license is not active|LicensingClient.*fail'; then
    echo "  ✘ 라이선스 없음 → unityhub → Preferences > Licenses > Add license > Personal"
    return 1
  fi
  echo "  ✔ Unity 배치 실행에서 라이선스 오류 없음"
}

check() {
  echo "[check] 헤드리스 실행 확인 ($UNITY_EXE)"
  need_unity || return 1
  out="$(as_user "$UNITY_EXE" -batchmode -quit -nographics -logFile - 2>&1)"
  code=$?
  printf '%s\n' "$out" | grep -iE 'licens|entitlement' | grep -viE 'debug' | tail -3 | sed 's/^/  /'
  [ $code -eq 0 ] && echo "  ✔ Unity 배치 실행 정상" || { echo "  ✘ 종료 코드 $code"; return 1; }
}

cloudflare() {
  echo "[cloudflare] wrangler 로그인 (배포용)"
  ensure_node || return 1
  if [ -n "${CLOUDFLARE_API_TOKEN:-}" ] || nvm_run "npx --yes wrangler whoami" 2>/dev/null | grep -q 'You are logged in'; then
    echo "  ✔ Cloudflare 인증 있음"
    return 0
  fi
  echo "  ✘ 인증 없음 → 원격 데스크톱 터미널에서: source ~/.nvm/nvm.sh && cd ~/workspace/YangVil && npx wrangler login"
  echo "    (또는 GitHub 시크릿 CLOUDFLARE_API_TOKEN 등록)"
  return 1
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
  local editor_root
  editor_root="$(dirname "$(dirname "$(dirname "$UNITY_EXE")")")"
  printf 'UNITY_EDITOR_ROOT=%s\nUNITY_EXTRA_ARGS=-nographics\nHOME=%s\nYANGVIL_CHECK_DIR=%s\n' "$editor_root" "$TARGET_HOME" "$TARGET_HOME/YangVil-check" | as_user tee .env >/dev/null
  [ -n "${NODE_BIN:-}" ] || ensure_node || return 1
  printf '%s:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin\n' "$NODE_BIN" | as_user tee .path >/dev/null
  echo "  러너 PATH에 nvm Node 추가: $NODE_BIN"
  sudo ./svc.sh install "$TARGET_USER" >/dev/null 2>&1 || true
  sudo ./svc.sh start
  sudo ./svc.sh status | grep -m1 -E 'Active:' | sed 's/^ */  /'
}

case "${1:-all}" in
  deps) deps ;;
  node) ensure_node ;;
  hub) hub ;;
  editor) editor ;;
  license) license ;;
  check) check ;;
  cloudflare) cloudflare ;;
  register) shift; register "${1:-}" ;;
  install) deps && hub && editor && license && check ;;
  sync) git -C "$REPO_ROOT" config alias.sync '!SYNC_FOLLOW_REMOTE=1 bash scripts/tools/sync.sh'
        git -C "$REPO_ROOT" config pull.ff only
        SYNC_FOLLOW_REMOTE=1 bash "$(dirname "$0")/sync.sh" ;;
  ci) SYNC_FOLLOW_REMOTE=1 bash "$(dirname "$0")/sync.sh"; license && check && cloudflare && register "${2:-}" ;;
  all) SYNC_FOLLOW_REMOTE=1 bash "$(dirname "$0")/sync.sh"; deps && hub && editor && license && check && cloudflare && register "${2:-}" ;;
  *) echo "사용법: bash $0 [install|license|check|cloudflare|register [토큰]|ci|all]"; exit 1 ;;
esac
