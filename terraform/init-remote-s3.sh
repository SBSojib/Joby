#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

usage() {
  echo "Usage: $0 -b <state-bucket> -r <aws-region> [-k <state-key>] [-d <dynamodb-lock-table>]"
}

bucket=""
region=""
key="joby/dev/terraform.tfstate"
dynamodb_table=""

while getopts ":b:r:k:d:h" opt; do
  case "$opt" in
    b) bucket="$OPTARG" ;;
    r) region="$OPTARG" ;;
    k) key="$OPTARG" ;;
    d) dynamodb_table="$OPTARG" ;;
    h)
      usage
      exit 0
      ;;
    :)
      echo "Missing value for -$OPTARG"
      usage
      exit 1
      ;;
    \?)
      echo "Unknown option: -$OPTARG"
      usage
      exit 1
      ;;
  esac
done

if [[ -z "$bucket" || -z "$region" ]]; then
  usage
  exit 1
fi

cp backend.generated.tf.example backend.generated.tf

backend_config=(
  "-backend-config=bucket=${bucket}"
  "-backend-config=key=${key}"
  "-backend-config=region=${region}"
)

if [[ -n "$dynamodb_table" ]]; then
  backend_config+=("-backend-config=dynamodb_table=${dynamodb_table}")
fi

terraform init -reconfigure "${backend_config[@]}"
