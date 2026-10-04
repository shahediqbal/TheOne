#!/usr/bin/env bash
set -euo pipefail

APP_PREFIX="${THEONE_API_CONTAINER_PREFIX:-pcliiwn3ayhlvmtqpax96hgy-}"
NETWORK="${THEONE_PROXY_NETWORK:-theone-staging-proxy}"
ALIAS="${THEONE_API_NETWORK_ALIAS:-theone-api-staging}"

container="$(
  docker ps --format '{{.Names}}' |
  awk -v prefix="$APP_PREFIX" 'index($0, prefix) == 1 { print; exit }'
)"

if [ -z "$container" ]; then
  echo "TheOne staging API container not found."
  exit 0
fi

if ! docker network inspect "$NETWORK" >/dev/null 2>&1; then
  echo "Required Docker network does not exist: $NETWORK" >&2
  exit 1
fi

if docker inspect "$container"   --format '{{range $name, $_ := .NetworkSettings.Networks}}{{println $name}}{{end}}' |
  grep -Fxq "$NETWORK"; then
  echo "$container is already attached to $NETWORK."
  exit 0
fi

docker network connect --alias "$ALIAS" "$NETWORK" "$container"
echo "Attached $container to $NETWORK as $ALIAS."
