# syntax=docker/dockerfile:1

FROM ghcr.io/linuxserver/baseimage-alpine:3.23

# set version label
ARG BUILD_DATE
ARG VERSION
ARG GAMARR_RELEASE
LABEL build_version="Gamarr version:- ${VERSION} Build-date:- ${BUILD_DATE}"
LABEL maintainer="gamarr-app"
LABEL org.opencontainers.image.source="https://github.com/gamarr-app/Gamarr"
LABEL org.opencontainers.image.url="https://github.com/gamarr-app/Gamarr"
LABEL org.opencontainers.image.description="A game collection manager for Usenet and BitTorrent users."

# environment settings
#
# TMPDIR points at a runtime directory that the s6 init creates at *container
# start*, so it does not exist while this image is being built. Anything in the
# build below that shells out to `mktemp` (notably dotnet-install.sh) must
# override TMPDIR for its own invocation, or it fails with
# "failed to create file via template".
ARG GAMARR_BRANCH="main"
ENV XDG_CONFIG_HOME="/config/xdg" \
  COMPlus_EnableDiagnostics=0 \
  TMPDIR=/run/gamarr-temp \
  DOTNET_ROOT=/usr/share/dotnet \
  PATH="/usr/share/dotnet:${PATH}"

# The pinned ASP.NET Core runtime version lives in ONE place:
# src/Directory.Build.props's <RuntimeFrameworkVersion> (a deliberate
# CVE-driven pin — see the comment on that line). Alpine's own
# `aspnetcore10-runtime` apk package lags Microsoft's patch releases (that lag
# is what took production down: the build demanded 10.0.11, Alpine 3.23 only
# ships 10.0.10-r0, and a framework-dependent app refuses to roll backward).
# Rather than hardcode the version a second time here where it can drift, we
# copy the source-of-truth file into the build context and read it below.
# Bumping the pin in Directory.Build.props is the only step needed to move
# this image to a newer runtime on the next build.
COPY src/Directory.Build.props /tmp/Directory.Build.props

RUN \
  echo "**** install packages ****" && \
  apk add -U --upgrade --no-cache \
    ca-certificates \
    clamav \
    clamav-daemon \
    clamav-libunrar \
    icu-data-full \
    icu-libs \
    krb5 \
    libgcc \
    libssl3 \
    libstdc++ \
    sqlite-libs \
    tzdata \
    xmlstarlet && \
  echo "**** install gamarr ****" && \
  mkdir -p /app/gamarr/bin && \
  if [ -z ${GAMARR_RELEASE+x} ]; then \
    GAMARR_RELEASE=$(curl -sL "https://api.github.com/repos/gamarr-app/Gamarr/releases/latest" \
    | jq -r '.tag_name' | sed 's/^v//'); \
  fi && \
  ARCH=$(uname -m) && \
  case "$ARCH" in \
    x86_64) RUNTIME="linux-musl-x64"; DOTNET_ARCH="x64" ;; \
    aarch64) RUNTIME="linux-musl-arm64"; DOTNET_ARCH="arm64" ;; \
    armv7l) RUNTIME="linux-musl-arm"; DOTNET_ARCH="arm" ;; \
    *) echo "Unsupported architecture: $ARCH" && exit 1 ;; \
  esac && \
  echo "Downloading Gamarr ${GAMARR_RELEASE} for ${RUNTIME}" && \
  curl -o \
    /tmp/gamarr.tar.gz -L \
    "https://github.com/gamarr-app/Gamarr/releases/download/v${GAMARR_RELEASE}/Gamarr.${GAMARR_RELEASE}.${RUNTIME}.tar.gz" && \
  tar xzf \
    /tmp/gamarr.tar.gz -C \
    /app/gamarr/bin --strip-components=1 && \
  echo -e "UpdateMethod=docker\nBranch=${GAMARR_BRANCH}\nPackageVersion=${VERSION}\nPackageAuthor=[gamarr-app](https://github.com/gamarr-app)" > /app/gamarr/package_info && \
  printf "Gamarr version: ${VERSION}\nBuild-date: ${BUILD_DATE}" > /build_version && \
  echo "**** install pinned ASP.NET Core runtime (${DOTNET_ARCH}, musl) ****" && \
  ASPNETCORE_VERSION=$(sed -n 's:.*<RuntimeFrameworkVersion>\([0-9.]*\)</RuntimeFrameworkVersion>.*:\1:p' /tmp/Directory.Build.props | head -n1) && \
  if [ -z "$ASPNETCORE_VERSION" ]; then \
    echo "ERROR: could not read <RuntimeFrameworkVersion> from Directory.Build.props" && exit 1; \
  fi && \
  echo "Pinned ASP.NET Core runtime version: ${ASPNETCORE_VERSION}" && \
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && \
  chmod +x /tmp/dotnet-install.sh && \
  TMPDIR=/tmp /tmp/dotnet-install.sh \
    --runtime aspnetcore \
    --version "${ASPNETCORE_VERSION}" \
    --os linux-musl \
    --architecture "${DOTNET_ARCH}" \
    --install-dir "${DOTNET_ROOT}" \
    --no-path \
    --verbose && \
  ln -sf "${DOTNET_ROOT}/dotnet" /usr/bin/dotnet && \
  mkdir -p /etc/dotnet && \
  echo "${DOTNET_ROOT}/" > /etc/dotnet/install_location && \
  echo "**** verify installed runtime meets the pin (fail the build, not the user's compose pull) ****" && \
  INSTALLED_VERSION=$("${DOTNET_ROOT}/dotnet" --list-runtimes | awk '$1 == "Microsoft.AspNetCore.App" {print $2}') && \
  if [ -z "$INSTALLED_VERSION" ]; then \
    echo "ERROR: dotnet --list-runtimes reports no Microsoft.AspNetCore.App runtime after install" && exit 1; \
  fi && \
  echo "Installed ASP.NET Core runtime version: ${INSTALLED_VERSION}" && \
  LOWEST=$(printf '%s\n%s\n' "$ASPNETCORE_VERSION" "$INSTALLED_VERSION" | sort -V | head -n1) && \
  if [ "$LOWEST" != "$ASPNETCORE_VERSION" ]; then \
    echo "ERROR: installed ASP.NET Core runtime ${INSTALLED_VERSION} is older than the pinned ${ASPNETCORE_VERSION}" && exit 1; \
  fi && \
  echo "**** cleanup ****" && \
  rm -rf \
    /app/gamarr/bin/Gamarr.Update \
    /tmp/*

# copy local files
COPY docker/root/ /

# ports and volumes
EXPOSE 6767

VOLUME /config

# svc-gamarr/data/check already resolves the configured port from config.xml
# (falling back to 6767) and treats /ping's `{"status":"OK"}` body as the
# liveness signal for s6-notifyoncheck; reuse it here instead of duplicating
# that logic so a dead Gamarr process (e.g. exited immediately for lack of a
# matching runtime) stops reporting "healthy" to `docker ps`/compose.
HEALTHCHECK --interval=30s --timeout=10s --start-period=120s --retries=3 \
  CMD /etc/s6-overlay/s6-rc.d/svc-gamarr/data/check
