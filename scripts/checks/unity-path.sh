#!/usr/bin/env bash

unity_version() {
  sed -n 's/^m_EditorVersion: //p' "$1/ProjectSettings/ProjectVersion.txt" | tr -d '\r'
}

unity_changeset() {
  sed -n 's/^m_EditorVersionWithRevision: .*(\(.*\)).*/\1/p' "$1/ProjectSettings/ProjectVersion.txt" | tr -d '\r'
}

unity_exe() {
  local version="$1" candidate
  for candidate in \
    "${UNITY_EDITOR_ROOT:-}/$version/Editor/Unity" \
    "${UNITY_EDITOR_ROOT:-}/$version/Editor/Unity.exe" \
    "$HOME/Unity/Hub/Editor/$version/Editor/Unity" \
    "/c/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe"; do
    [ -x "$candidate" ] && { echo "$candidate"; return 0; }
  done
  return 1
}

resolve_unity() {
  local project="$1" version
  version="$(unity_version "$project")"
  UNITY="$(unity_exe "$version")" || { echo "[unity] ✘ Unity $version 없음 (ProjectSettings/ProjectVersion.txt 기준)"; return 1; }
  echo "[unity] $version → $UNITY"
}
