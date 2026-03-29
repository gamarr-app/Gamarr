#!/bin/bash
# Check Sentry for new unresolved issues.
# Exits 0 with issue details on stdout if new issues found.
# Exits 1 if no new issues (nothing to do).

set -euo pipefail

SENTRY_TOKEN="${SENTRY_AUTH_TOKEN:?Set SENTRY_AUTH_TOKEN environment variable}"
SENTRY_ORG="gamarr"
SEEN_FILE=".sentry-seen-issues.json"

# Fetch unresolved issues
ISSUES=$(curl -sf -H "Authorization: Bearer ${SENTRY_TOKEN}" \
  "https://sentry.io/api/0/organizations/${SENTRY_ORG}/issues/?query=is:unresolved&sort=date&limit=10")

# Load seen IDs
if [ -f "$SEEN_FILE" ]; then
  SEEN=$(cat "$SEEN_FILE")
else
  SEEN="[]"
fi

# Find new issues (not in seen list) that are actual errors (not info-level)
NEW_ISSUES=$(echo "$ISSUES" | python3 -c "
import json, sys
issues = json.load(sys.stdin)
seen = json.loads('''${SEEN}''')
seen_ids = set(str(s) for s in seen)
new = [i for i in issues if str(i['id']) not in seen_ids and i['level'] == 'error']
if not new:
    sys.exit(0)
print(json.dumps([{'id': i['id'], 'title': i['title'], 'count': i['count'], 'firstSeen': i['firstSeen']} for i in new]))
")

if [ -z "$NEW_ISSUES" ] || [ "$NEW_ISSUES" = "null" ]; then
  exit 1
fi

# Fetch stack traces for each new issue
echo "$NEW_ISSUES" | python3 -c "
import json, sys, subprocess
issues = json.load(sys.stdin)
for issue in issues:
    iid = issue['id']
    result = subprocess.run(
        ['curl', '-sf', '-H', 'Authorization: Bearer ${SENTRY_TOKEN}',
         f'https://sentry.io/api/0/organizations/${SENTRY_ORG}/issues/{iid}/events/latest/'],
        capture_output=True, text=True)
    if result.returncode == 0:
        event = json.loads(result.stdout)
        issue['event'] = event
print(json.dumps(issues, indent=2))
"
