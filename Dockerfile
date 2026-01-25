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
ARG GAMARR_BRANCH="main"
ENV XDG_CONFIG_HOME="/config/xdg" \
  COMPlus_EnableDiagnostics=0 \
  TMPDIR=/run/gamarr-temp

RUN \
  echo "**** install packages ****" && \
  apk add -U --upgrade --no-cache \
    icu-libs \
    sqlite-libs \
    xmlstarlet && \
  echo "**** install gamarr ****" && \
  mkdir -p /app/gamarr/bin && \
  if [ -z ${GAMARR_RELEASE+x} ]; then \
    GAMARR_RELEASE=$(curl -sL "https://api.github.com/repos/gamarr-app/Gamarr/releases/latest" \
    | jq -r '.tag_name' | sed 's/^v//'); \
  fi && \
  ARCH=$(uname -m) && \
  case "$ARCH" in \
    x86_64) RUNTIME="linux-musl-x64" ;; \
    aarch64) RUNTIME="linux-musl-arm64" ;; \
    armv7l) RUNTIME="linux-musl-arm" ;; \
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
  echo "**** cleanup ****" && \
  rm -rf \
    /app/gamarr/bin/Gamarr.Update \
    /tmp/*

# copy local files
COPY docker/root/ /

# ports and volumes
EXPOSE 6767

VOLUME /config
