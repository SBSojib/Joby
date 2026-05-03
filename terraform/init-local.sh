#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"
rm -f backend.generated.tf
terraform init -reconfigure
