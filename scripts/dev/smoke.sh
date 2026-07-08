#!/bin/bash
# Throwaway local Gamarr instance for manual/agent verification.
#
#   scripts/dev/smoke.sh start [port]   # default 6968 (6767 is usually the live docker instance)
#   scripts/dev/smoke.sh stop
#   scripts/dev/smoke.sh status
#
# Uses _output/net10.0/Gamarr (run `make build` first). Data lives in
# /tmp/gamarr-dev; auth is disabled for localhost so the UI opens without
# login and the API key (printed on start) is all you need.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
DATA_DIR=/tmp/gamarr-dev
PID_FILE=$DATA_DIR/smoke.pid
LOG_FILE=$DATA_DIR/smoke.log
BINARY=$REPO_ROOT/_output/net10.0/Gamarr
PORT=${2:-6968}

export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"

stop_instance() {
  if [ -f "$PID_FILE" ] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
    kill "$(cat "$PID_FILE")"
    sleep 1
  fi
  pkill -f "Gamarr -nobrowser -data=$DATA_DIR" 2>/dev/null || true
}

case "${1:-start}" in
  stop)
    stop_instance
    if [ -f "$DATA_DIR/fake-torznab.pid" ]; then
      kill "$(cat "$DATA_DIR/fake-torznab.pid")" 2>/dev/null || true
    fi
    pkill -f "scripts/dev/fake-torznab.py" 2>/dev/null || true
    rm -rf "$DATA_DIR" /tmp/gamarr-dev-games
    echo "smoke: stopped and cleaned"
    ;;
  status)
    if [ -f "$PID_FILE" ] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
      KEY=$(sed -n 's/.*<ApiKey>\(.*\)<\/ApiKey>.*/\1/p' "$DATA_DIR/config.xml")
      PORT=$(sed -n 's/.*<Port>\(.*\)<\/Port>.*/\1/p' "$DATA_DIR/config.xml")
      echo "smoke: running — http://localhost:$PORT (api key: $KEY)"
    else
      echo "smoke: not running"
    fi
    ;;
  start)
    [ -x "$BINARY" ] || { echo "smoke: $BINARY not found — run 'make build' first"; exit 1; }
    stop_instance
    rm -rf "$DATA_DIR"
    mkdir -p "$DATA_DIR"
    cat > "$DATA_DIR/config.xml" <<EOF
<Config>
  <Port>$PORT</Port>
  <AuthenticationMethod>External</AuthenticationMethod>
  <AuthenticationRequired>DisabledForLocalAddresses</AuthenticationRequired>
</Config>
EOF
    nohup "$BINARY" -nobrowser -data="$DATA_DIR" > "$LOG_FILE" 2>&1 &
    echo $! > "$PID_FILE"
    for _ in $(seq 1 40); do
      KEY=$(sed -n 's/.*<ApiKey>\(.*\)<\/ApiKey>.*/\1/p' "$DATA_DIR/config.xml" 2>/dev/null || true)
      if [ -n "${KEY:-}" ] && curl -s -o /dev/null -w "%{http_code}" \
          -H "X-Api-Key: $KEY" "http://localhost:$PORT/api/v3/system/status" | grep -q 200; then
        echo "smoke: up — http://localhost:$PORT (api key: $KEY, log: $LOG_FILE)"
        exit 0
      fi
      sleep 1
    done
    echo "smoke: failed to start — tail of $LOG_FILE:"
    tail -20 "$LOG_FILE"
    exit 1
    ;;
  *)
    echo "usage: $0 {start [port]|stop|status}"
    exit 1
    ;;
esac
