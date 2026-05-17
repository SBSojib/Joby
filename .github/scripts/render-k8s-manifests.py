#!/usr/bin/env python3
"""Substitute deployment placeholders in rendered Kubernetes manifests."""

from __future__ import annotations

import os
from pathlib import Path
from types import SimpleNamespace

MANIFESTS = (
    "backend.yaml",
    "frontend.yaml",
    "configmap.yaml",
    "ingress.yaml",
    "serviceaccount.yaml",
    "clustersecretstore.yaml",
    "externalsecret.yaml",
)

REQUIRED_ENV = (
    "MANIFEST_WORKDIR",
    "BACKEND_IMAGE",
    "FRONTEND_IMAGE",
    "S3_BUCKET_NAME",
    "AWS_REGION",
    "MANIFEST_CORS_ORIGIN",
    "ORIGIN_HOSTNAME",
    "ACM_CERTIFICATE_ARN",
    "WAF_WEB_ACL_ARN",
    "BACKEND_IRSA_ROLE_ARN",
    "APPLICATION_SECRET_NAME",
)


def load_env(keys: tuple[str, ...]) -> SimpleNamespace:
    missing = [key for key in keys if not os.environ.get(key)]
    if missing:
        raise SystemExit(f"Missing environment variables: {', '.join(missing)}")
    return SimpleNamespace(**{key: os.environ[key] for key in keys})


def build_replacements(env: SimpleNamespace) -> dict[str, str]:
    return {
        "REPLACE_WITH_BACKEND_IMAGE": env.BACKEND_IMAGE,
        "REPLACE_WITH_FRONTEND_IMAGE": env.FRONTEND_IMAGE,
        "REPLACE_WITH_S3_BUCKET_NAME": env.S3_BUCKET_NAME,
        "REPLACE_WITH_AWS_REGION": env.AWS_REGION,
        "REPLACE_WITH_CORS_ALLOWED_ORIGIN": env.MANIFEST_CORS_ORIGIN,
        "REPLACE_WITH_ORIGIN_HOSTNAME": env.ORIGIN_HOSTNAME,
        "REPLACE_WITH_ACM_CERTIFICATE_ARN": env.ACM_CERTIFICATE_ARN,
        "REPLACE_WITH_WAF_WEB_ACL_ARN": env.WAF_WEB_ACL_ARN,
        "REPLACE_WITH_BACKEND_IRSA_ROLE_ARN": env.BACKEND_IRSA_ROLE_ARN,
        "REPLACE_WITH_APPLICATION_SECRET_NAME": env.APPLICATION_SECRET_NAME,
    }


def render_manifests(manifest_dir: Path, replacements: dict[str, str]) -> None:
    for manifest in MANIFESTS:
        path = manifest_dir / manifest
        contents = path.read_text(encoding="utf-8")
        for old, new in replacements.items():
            contents = contents.replace(old, new)
        path.write_text(contents, encoding="utf-8")


def main() -> None:
    env = load_env(REQUIRED_ENV)
    render_manifests(Path(env.MANIFEST_WORKDIR), build_replacements(env))


if __name__ == "__main__":
    main()
