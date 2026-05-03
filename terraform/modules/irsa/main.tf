locals {
  oidc_provider = replace(var.oidc_provider_url, "https://", "")
  role_name     = "${var.project_name}-${var.environment}-${var.service_account_name}"
}

data "aws_iam_policy_document" "assume_role" {
  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [var.oidc_provider_arn]
    }

    condition {
      test     = "StringEquals"
      variable = "${local.oidc_provider}:aud"
      values   = ["sts.amazonaws.com"]
    }

    condition {
      test     = "StringEquals"
      variable = "${local.oidc_provider}:sub"
      values   = ["system:serviceaccount:${var.namespace}:${var.service_account_name}"]
    }
  }
}

resource "aws_iam_role" "this" {
  name               = local.role_name
  assume_role_policy = data.aws_iam_policy_document.assume_role.json
  tags               = var.tags
}

data "aws_iam_policy_document" "s3_access" {
  statement {
    sid       = "ListUploadsBucket"
    actions   = ["s3:ListBucket"]
    resources = [var.s3_bucket_arn]
  }

  statement {
    sid = "ManageUploadObjects"
    actions = [
      "s3:GetObject",
      "s3:PutObject",
      "s3:DeleteObject",
    ]
    resources = ["${var.s3_bucket_arn}/*"]
  }
}

resource "aws_iam_role_policy" "s3_access" {
  name   = "${local.role_name}-s3-access"
  role   = aws_iam_role.this.id
  policy = data.aws_iam_policy_document.s3_access.json
}
