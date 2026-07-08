#!/bin/bash
# Seed the smoke instance (scripts/dev/smoke.sh) with a realistic library:
# ten well-known Steam games, one imported game file, and a fake Torznab
# indexer (scripts/dev/fake-torznab.py, started on :9899 if not running) so
# interactive search returns rows. Idempotent-ish: re-running re-adds nothing
# that already exists (API returns 400s which are ignored).
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
DATA_DIR=/tmp/gamarr-dev
GAMES_DIR=/tmp/gamarr-dev-games

KEY=$(sed -n 's/.*<ApiKey>\(.*\)<\/ApiKey>.*/\1/p' "$DATA_DIR/config.xml" 2>/dev/null || true)
PORT=$(sed -n 's/.*<Port>\(.*\)<\/Port>.*/\1/p' "$DATA_DIR/config.xml" 2>/dev/null || true)
[ -n "$KEY" ] || { echo "seed: smoke instance not running — 'make smoke' first"; exit 1; }
BASE="http://localhost:$PORT/api/v3"
H="X-Api-Key: $KEY"

# fake torznab
if ! curl -s -o /dev/null "http://localhost:9899/api?t=caps"; then
  nohup python3 "$REPO_ROOT/scripts/dev/fake-torznab.py" > /dev/null 2>&1 &
  echo $! > "$DATA_DIR/fake-torznab.pid"
  sleep 1
fi

mkdir -p "$GAMES_DIR"
curl -s -X POST "$BASE/rootfolder" -H "$H" -H "Content-Type: application/json" \
  -d "{\"path\": \"$GAMES_DIR\"}" > /dev/null

for APPID in 1145360 504230 413150 367520 1086940 1245620 620 105600 892970 739630; do
  LOOKUP=$(curl -s "$BASE/game/lookup?term=steam:$APPID" -H "$H")
  GAME=$(echo "$LOOKUP" | python3 -c "
import json, sys
r = json.load(sys.stdin)
if not r:
    sys.exit(1)
g = r[0]
g['rootFolderPath'] = '$GAMES_DIR'
g['qualityProfileId'] = 1
g['monitored'] = True
g['addOptions'] = {'searchForGame': False}
print(json.dumps(g))") || { echo "seed: no result for steam:$APPID"; continue; }
  CODE=$(echo "$GAME" | curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE/game" -H "$H" -H "Content-Type: application/json" -d @-)
  echo "seed: steam:$APPID -> $CODE"
  sleep 1.2 # stay under the Steam Store 1 req/sec throttle
done

curl -s -o /dev/null -w "seed: indexer -> %{http_code}\n" -X POST "$BASE/indexer" -H "$H" -H "Content-Type: application/json" -d '{
  "enableRss": false, "enableAutomaticSearch": true, "enableInteractiveSearch": true,
  "name": "FakeTorznab", "implementation": "Torznab", "configContract": "TorznabSettings",
  "protocol": "torrent", "priority": 25,
  "fields": [
    {"name": "baseUrl", "value": "http://localhost:9899"},
    {"name": "apiPath", "value": "/api"},
    {"name": "apiKey", "value": ""},
    {"name": "categories", "value": [4000, 4050]},
    {"name": "minimumSeeders", "value": 1}
  ]
}'

# give the first game (Hades) an importable file so files/downloaded states render
GPATH=$(curl -s "$BASE/game/1" -H "$H" | python3 -c "import json,sys; print(json.load(sys.stdin).get('path',''))" 2>/dev/null || true)
if [ -n "$GPATH" ]; then
  mkdir -p "$GPATH"
  [ -f "$GPATH/game.iso" ] || dd if=/dev/zero of="$GPATH/game.iso" bs=1m count=64 2>/dev/null
  curl -s -o /dev/null -w "seed: rescan -> %{http_code}\n" -X POST "$BASE/command" -H "$H" \
    -H "Content-Type: application/json" -d '{"name": "RescanGame", "gameId": 1}'
fi

echo "seed: done — http://localhost:$PORT"
