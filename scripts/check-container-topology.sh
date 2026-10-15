#!/usr/bin/env bash
# Assert the invariants that tie the Dockerfile, the s6 service scripts and the
# application's own defaults together.
#
# Docker is how essentially every Gamarr install runs, so a drift between these
# three places is a production outage that no unit test sees: the backend tests
# never read the Dockerfile, and the image build only fails if a *command*
# fails, not if a path stops matching. Each check below exists because the
# matching drift has either broken a release or come one edit away from it.
#
# Pure static analysis of the repo — no Docker daemon, no network, runs on any
# platform in well under a second.

set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOCKERFILE="$REPO_ROOT/Dockerfile"
ROOTFS="$REPO_ROOT/docker/root"
S6_DIR="$ROOTFS/etc/s6-overlay/s6-rc.d"
GAMARR_RUN="$S6_DIR/svc-gamarr/run"

failures=0

fail() {
  printf 'FAIL: %s\n' "$1" >&2
  failures=$((failures + 1))
}

pass() {
  printf '  ok: %s\n' "$1"
}

for required in "$DOCKERFILE" "$GAMARR_RUN"; do
  if [[ ! -f $required ]]; then
    fail "expected file is missing: ${required#"$REPO_ROOT"/}"
    exit 1
  fi
done

# ---------------------------------------------------------------------------
# 1. TMPDIR: the Dockerfile points the app at a runtime directory that does not
#    exist in the image. The service script has to create it on every start, so
#    the two paths must agree. If they drift the app starts with a TMPDIR that
#    is not there and fails on its first temp write, long after the build went
#    green.
# ---------------------------------------------------------------------------
env_tmpdir=$(sed -n 's/^[[:space:]]*TMPDIR=\([^ \\]*\).*/\1/p' "$DOCKERFILE" | head -n1)
run_tmpdir=$(sed -n 's|^[[:space:]]*mkdir -p \(/run/[^ ]*\).*|\1|p' "$GAMARR_RUN" | head -n1)

if [[ -z $env_tmpdir ]]; then
  fail "no TMPDIR= found in the Dockerfile ENV block"
elif [[ -z $run_tmpdir ]]; then
  fail "svc-gamarr/run does not 'mkdir -p' any /run directory, but the Dockerfile sets TMPDIR=$env_tmpdir"
elif [[ $env_tmpdir != "$run_tmpdir" ]]; then
  fail "TMPDIR drift: Dockerfile sets '$env_tmpdir' but svc-gamarr/run creates '$run_tmpdir'"
else
  pass "TMPDIR ($env_tmpdir) is created by svc-gamarr/run"
fi

# ---------------------------------------------------------------------------
# 2. ...and because that TMPDIR does not exist *during the build*, anything in
#    the build that shells out to mktemp must override it for its own
#    invocation. dotnet-install.sh is the one that bit us: it dies with
#    "failed to create file via template".
# ---------------------------------------------------------------------------
tmpdir_problems=0
while IFS= read -r line; do
  # Strip the line number grep -n prepends, then skip prose.
  body=${line#*:}
  body=${body#"${body%%[![:space:]]*}"}
  [[ $body == '#'* ]] && continue
  # The curl that downloads the installer and the chmod that arms it do not run it.
  [[ $body == *'curl'* ]] && continue
  [[ $body == *'chmod'* ]] && continue
  if [[ $body != *'TMPDIR=/tmp'* ]]; then
    fail "Dockerfile invokes a temp-using build tool without 'TMPDIR=/tmp': $line"
    tmpdir_problems=$((tmpdir_problems + 1))
  fi
done < <(grep -n 'dotnet-install\.sh\|mktemp' "$DOCKERFILE")
[[ $tmpdir_problems -eq 0 ]] && pass "build-time temp users override TMPDIR"

# ---------------------------------------------------------------------------
# 3. The -data directory the service starts Gamarr with must be a declared
#    VOLUME. If it is not, every byte of the user's config and database lives
#    in the container's writable layer and is destroyed by the next
#    `docker compose pull && up -d` — silently, with no error anywhere.
# ---------------------------------------------------------------------------
data_dir=$(sed -n 's/.*-data=\([^ \\]*\).*/\1/p' "$GAMARR_RUN" | head -n1)
volumes=$(sed -n 's/^VOLUME[[:space:]]*\(.*\)/\1/p' "$DOCKERFILE" | tr -d '[]",' | tr ' ' '\n' | sed '/^$/d')

if [[ -z $data_dir ]]; then
  fail "svc-gamarr/run passes no -data= argument"
elif [[ -z $volumes ]]; then
  fail "Dockerfile declares no VOLUME, but the service stores data in $data_dir"
elif ! printf '%s\n' "$volumes" | grep -qx "$data_dir"; then
  fail "data dir '$data_dir' is not a declared VOLUME (found: $(echo "$volumes" | tr '\n' ' ')) — user config would not survive a container recreate"
else
  pass "data dir ($data_dir) is a declared VOLUME"
fi

# ---------------------------------------------------------------------------
# 4. The HEALTHCHECK runs a script out of the s6 tree rather than duplicating
#    the port-resolution logic. That only works while the path exists and is
#    executable; a rename makes every container report unhealthy forever.
# ---------------------------------------------------------------------------
health_cmd=$(grep -A2 '^HEALTHCHECK' "$DOCKERFILE" | sed -n 's|.*\(/etc/s6-overlay/[^ ]*\).*|\1|p' | head -n1)
if [[ -z $health_cmd ]]; then
  fail "HEALTHCHECK does not reference a script under /etc/s6-overlay"
else
  health_file="$ROOTFS$health_cmd"
  if [[ ! -f $health_file ]]; then
    fail "HEALTHCHECK runs '$health_cmd' but docker/root$health_cmd does not exist"
  elif [[ ! -x $health_file ]]; then
    fail "HEALTHCHECK script docker/root$health_cmd is not executable"
  else
    pass "HEALTHCHECK script exists and is executable"
  fi
fi

# ---------------------------------------------------------------------------
# 5. Three independent copies of the default port: EXPOSE, the healthcheck's
#    fallback for a container with no config.xml yet, and the application
#    default. A first-run container with a mismatch never reports healthy.
# ---------------------------------------------------------------------------
expose_port=$(sed -n 's/^EXPOSE[[:space:]]*\([0-9]*\).*/\1/p' "$DOCKERFILE" | head -n1)
check_file="$S6_DIR/svc-gamarr/data/check"
check_port=$(sed -n 's/.*PORT:-\([0-9]*\)}.*/\1/p' "$check_file" 2>/dev/null | head -n1)
config_port=$(sed -n 's/.*GetValueInt("Port",[[:space:]]*\([0-9]*\)).*/\1/p' \
  "$REPO_ROOT/src/NzbDrone.Core/Configuration/ConfigFileProvider.cs" 2>/dev/null | head -n1)

if [[ -z $expose_port || -z $check_port || -z $config_port ]]; then
  fail "could not read all three default ports (EXPOSE='$expose_port' healthcheck='$check_port' ConfigFileProvider='$config_port')"
elif [[ $expose_port != "$check_port" || $expose_port != "$config_port" ]]; then
  fail "default port drift: EXPOSE=$expose_port, healthcheck fallback=$check_port, ConfigFileProvider=$config_port"
else
  pass "default port ($expose_port) agrees across EXPOSE, healthcheck and ConfigFileProvider"
fi

# ---------------------------------------------------------------------------
# 6. The image deletes the updater binary and stamps UpdateMethod=docker into
#    package_info. Those two go together: the stamp is what stops the app
#    trying to update itself. Keep the delete without the stamp and the
#    in-app updater runs against a binary that is not there.
# ---------------------------------------------------------------------------
removes_updater=$(grep -c 'Gamarr\.Update' "$DOCKERFILE")
declares_docker=$(grep -c 'UpdateMethod=docker' "$DOCKERFILE")

if [[ $removes_updater -gt 0 && $declares_docker -eq 0 ]]; then
  fail "Dockerfile removes Gamarr.Update but never writes UpdateMethod=docker — the in-app updater would run against a missing binary"
elif [[ $removes_updater -eq 0 && $declares_docker -gt 0 ]]; then
  fail "Dockerfile writes UpdateMethod=docker but ships Gamarr.Update — harmless, but one of the two is now stale"
else
  pass "updater removal and UpdateMethod=docker agree"
fi

# ---------------------------------------------------------------------------
# 7. s6 refuses to start a service whose run script is not executable, and a
#    missing shebang makes it exec the file as the wrong interpreter. Both are
#    easy to lose through a patch, a zip round-trip or a Windows checkout.
# ---------------------------------------------------------------------------
script_problems=0
while IFS= read -r script; do
  rel=${script#"$REPO_ROOT"/}
  if [[ ! -x $script ]]; then
    fail "s6 script is not executable: $rel"
    script_problems=$((script_problems + 1))
  fi
  if [[ $(head -c2 "$script") != '#!' ]]; then
    fail "s6 script has no shebang: $rel"
    script_problems=$((script_problems + 1))
  fi
done < <(find "$S6_DIR" -type f \( -name run -o -name finish -o -name check \) 2>/dev/null)
[[ $script_problems -eq 0 ]] && pass "every s6 run/finish/check script is executable and has a shebang"

# ---------------------------------------------------------------------------
# 8. A oneshot's `up` file is a command line, and ours delegate to the sibling
#    `run` script. A typo there fails at container start, not at build.
# ---------------------------------------------------------------------------
up_problems=0
while IFS= read -r up; do
  rel=${up#"$REPO_ROOT"/}
  target=$(tr -d '\r' <"$up" | sed -n 's|^[[:space:]]*\(/etc/s6-overlay/[^ ]*\).*|\1|p' | head -n1)
  if [[ -n $target && ! -f "$ROOTFS$target" ]]; then
    fail "$rel points at '$target', which does not exist under docker/root"
    up_problems=$((up_problems + 1))
  fi
done < <(find "$S6_DIR" -type f -name up 2>/dev/null)
[[ $up_problems -eq 0 ]] && pass "every s6 'up' file points at a script that exists"

# ---------------------------------------------------------------------------
# 9. svc-gamarr/run has one branch for root and one for LSIO_NON_ROOT_USER.
#    They must start Gamarr with the same arguments — a flag added to only one
#    of them is a bug that shows up for exactly half the user base.
# ---------------------------------------------------------------------------
# The arguments trail the binary on the next line, so flatten the file first and
# read each invocation's flags up to the end of its exec block.
first_args=""
second_args=""
arg_count=0
while IFS= read -r args; do
  arg_count=$((arg_count + 1))
  if [[ $arg_count -eq 1 ]]; then
    first_args=$args
  elif [[ $arg_count -eq 2 ]]; then
    second_args=$args
  fi
done < <(sed 's/\\$//' "$GAMARR_RUN" | tr '\n' ' ' |
  grep -oE '/app/gamarr/bin/Gamarr( +-[^ ]+)+' |
  sed 's|/app/gamarr/bin/Gamarr||' |
  sed 's/  */ /g' |
  sed 's/^ //; s/ *$//')

if [[ $arg_count -lt 2 ]]; then
  pass "svc-gamarr/run has a single start path (nothing to compare)"
elif [[ $first_args != "$second_args" ]]; then
  fail "svc-gamarr/run starts Gamarr differently for root and non-root: '$first_args' vs '$second_args'"
else
  pass "root and non-root branches start Gamarr with the same arguments ($first_args)"
fi

# ---------------------------------------------------------------------------
if [[ $failures -gt 0 ]]; then
  printf '\ncontainer topology: FAILED (%d problem(s))\n' "$failures" >&2
  exit 1
fi

printf '\ncontainer topology: OK\n'
