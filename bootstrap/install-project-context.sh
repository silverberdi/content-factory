#!/usr/bin/env bash
set -euo pipefail

if [[ ! -d openspec ]]; then
  echo "OpenSpec is not initialized. Run the commands guide first."
  exit 1
fi

cp bootstrap/openspec/config.yaml openspec/config.yaml

mkdir -p openspec/changes/foundation-access-control-center
cp -R bootstrap/openspec/changes/foundation-access-control-center/. \
      openspec/changes/foundation-access-control-center/

echo "Canonical OpenSpec config and initial change installed."
echo "Next: openspec validate foundation-access-control-center"
