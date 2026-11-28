#!/usr/bin/env bash
# Smoke-test a built Gamarr Docker image the way nearly every user runs it:
# the s6 init as root, PUID/PGID dropping to a non-root user, and /config on a
# volume mapped from the host.
#
# Usage: scripts/docker-smoke-test.sh <image> [host-port]
#
# What it proves, in order:
#   1. the container boots and /ping answers (no missing runtime, no restart loop)
#   2. the API key Gamarr wrote to /config/config.xml opens /api/v3/system/status,
#      and the app reports it is running in Docker with /config as its data dir
#   3. a DB-backed write (tag) and a settings write (media management) are made
#   4. the container is destroyed and recreated on the same /config volume, and
#      the InstallId, the API key and both writes are all still there
#
# Needs docker, curl and jq (all present on GitHub's ubuntu runners).

set -uo pipefail

IMAGE="${1:?usage: $0 <image> [host-port]}"
PORT="${2:-16767}"
NAME="gamarr-smoke-$$"
CONFIG_DIR="$(mktemp -d)"
BASE="http://localhost:${PORT}"
MARKER_DAYS=17
TAG_LABEL="smoke-persist"
API_KEY=""

dump_logs() {
  echo "---- docker logs ($NAME) ----" >&2
  docker logs "$NAME" 2>&1 | tail -n 80 >&2 || true
}

cleanup() {
  docker rm -f "$NAME" >/dev/null 2>&1 || true
  rm -rf "$CONFIG_DIR" 2>/dev/null || true
}
trap cleanup EXIT

die() {
  echo "::error::$1" >&2
  dump_logs
  exit 1
}

start_container() {
  docker run -d --name "$NAME" \
    -e PUID="$(id -u)" -e PGID="$(id -g)" -e TZ=UTC \
    -v "${CONFIG_DIR}:/config" \
    -p "${PORT}:6767" \
    "$IMAGE" >/dev/null || die "docker run failed"
}

wait_for_ping() {
  echo "Waiting for ${BASE}/ping (up to 120s)..."
  for i in $(seq 1 60); do
    if curl -fsS "${BASE}/ping" 2>/dev/null | grep -q '"status"[[:space:]]*:[[:space:]]*"OK"'; then
      echo "  answered after ~$((i * 2))s"
      return 0
    fi
    if ! docker ps --filter "name=${NAME}" --filter status=running -q | grep -q .; then
      die "container exited before /ping answered"
    fi
    # s6 restarts the app in place, so a fatal startup failure leaves the
    # container "running" forever. Bail on the repeating hostfxr banner.
    if [ "$(docker logs "$NAME" 2>&1 | grep -c 'The framework .* was not found')" -ge 2 ]; then
      die "restart loop: no .NET runtime in the image satisfies the pinned version"
    fi
    sleep 2
  done
  die "/ping never answered within 120s"
}

config_value() {
  sed -n "s:.*<$1>\(.*\)</$1>.*:\1:p" "${CONFIG_DIR}/config.xml" | head -n1
}

api() {
  # api METHOD PATH [json-body]
  local method="$1" path="$2" body="${3:-}"
  if [ -n "$body" ]; then
    curl -fsS -X "$method" -H "X-Api-Key: ${API_KEY}" -H 'Content-Type: application/json' \
      --data "$body" "${BASE}${path}"
  else
    curl -fsS -X "$method" -H "X-Api-Key: ${API_KEY}" "${BASE}${path}"
  fi
}

echo "== first start =="
start_container
wait_for_ping

[ -f "${CONFIG_DIR}/config.xml" ] || die "config.xml was not written to the mapped /config volume"
API_KEY="$(config_value ApiKey)"
INSTALL_ID="$(config_value InstallId)"
[ -n "$API_KEY" ] || die "no <ApiKey> in config.xml"
[ -n "$INSTALL_ID" ] || die "no <InstallId> in config.xml"
echo "  api key and install id present in config.xml"

STATUS="$(api GET /api/v3/system/status)" || die "/api/v3/system/status failed with the config.xml API key"
echo "$STATUS" | jq -e '.appData == "/config"' >/dev/null || die "appData is not /config: $(echo "$STATUS" | jq -r .appData)"
echo "$STATUS" | jq -e '.isLinux == true and .isDocker == true' >/dev/null || die "status does not report Linux + Docker: $STATUS"
echo "  system/status ok ($(echo "$STATUS" | jq -r .version))"

api POST /api/v3/tag "{\"label\":\"${TAG_LABEL}\"}" >/dev/null || die "could not create a tag"
MM="$(api GET /api/v3/config/mediamanagement)" || die "could not read media management config"
api PUT /api/v3/config/mediamanagement "$(echo "$MM" | jq -c ".recycleBinCleanupDays = ${MARKER_DAYS}")" >/dev/null \
  || die "could not save media management config"
echo "  wrote tag and media management setting"

echo "== recreate container on the same /config volume =="
docker stop -t 30 "$NAME" >/dev/null || die "docker stop failed"
docker rm "$NAME" >/dev/null || die "docker rm failed"
start_container
wait_for_ping

[ "$(config_value InstallId)" = "$INSTALL_ID" ] || die "InstallId changed across recreate: ${INSTALL_ID} -> $(config_value InstallId)"
[ "$(config_value ApiKey)" = "$API_KEY" ] || die "ApiKey changed across recreate"
echo "  InstallId and ApiKey unchanged"

api GET /api/v3/system/status >/dev/null || die "status failed after recreate"
api GET /api/v3/tag | jq -e "map(select(.label == \"${TAG_LABEL}\")) | length == 1" >/dev/null \
  || die "tag did not survive the recreate (database not persisted)"
api GET /api/v3/config/mediamanagement | jq -e ".recycleBinCleanupDays == ${MARKER_DAYS}" >/dev/null \
  || die "media management setting did not survive the recreate"
echo "  tag and setting persisted"

echo "Docker smoke test passed."
