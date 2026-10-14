#!/usr/bin/env bash
set -euo pipefail

base_url="${1:?Usage: bash tests/smoke.sh BASE_URL}"
base_url="${base_url%/}"

health="$(curl --fail --silent --show-error --connect-timeout 10 --max-time 30 \
  --retry 12 --retry-delay 5 --retry-all-errors "$base_url/health")"
[[ "$health" == "Healthy" ]] || { echo "Unexpected health response: $health"; exit 1; }

home="$(curl --fail --silent --show-error --connect-timeout 10 --max-time 30 \
  --retry 12 --retry-delay 5 --retry-all-errors "$base_url/")"
[[ "$home" == "Hello from Azure Container Apps!" ]] || { echo "Unexpected home response: $home"; exit 1; }
echo "Smoke checks passed for $base_url"
