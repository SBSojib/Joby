locals {
  name_prefix   = "${var.project_name}-${var.environment}"
  oidc_provider = replace(var.oidc_provider_url, "https://", "")
}

data "aws_iam_policy_document" "assume_role" {
  for_each = {
    aws_load_balancer_controller = "system:serviceaccount:${var.aws_load_balancer_controller_namespace}:aws-load-balancer-controller"
    external_secrets             = "system:serviceaccount:${var.external_secrets_namespace}:external-secrets"
    external_dns                 = "system:serviceaccount:${var.external_dns_namespace}:external-dns"
    cluster_autoscaler           = "system:serviceaccount:kube-system:cluster-autoscaler"
    fluent_bit                   = "system:serviceaccount:${var.observability_namespace}:aws-for-fluent-bit"
  }

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
      values   = [each.value]
    }
  }
}

resource "aws_iam_role" "addon" {
  for_each = data.aws_iam_policy_document.assume_role

  name               = "${local.name_prefix}-${replace(each.key, "_", "-")}"
  assume_role_policy = each.value.json
  tags               = var.tags
}

data "aws_iam_policy_document" "aws_load_balancer_controller" {
  statement {
    actions = [
      "iam:CreateServiceLinkedRole",
      "ec2:DescribeAccountAttributes",
      "ec2:DescribeAddresses",
      "ec2:DescribeAvailabilityZones",
      "ec2:DescribeInternetGateways",
      "ec2:DescribeVpcs",
      "ec2:DescribeVpcPeeringConnections",
      "ec2:DescribeSubnets",
      "ec2:DescribeSecurityGroups",
      "ec2:DescribeInstances",
      "ec2:DescribeNetworkInterfaces",
      "ec2:DescribeTags",
      "ec2:GetCoipPoolUsage",
      "ec2:DescribeCoipPools",
      "elasticloadbalancing:DescribeLoadBalancers",
      "elasticloadbalancing:DescribeLoadBalancerAttributes",
      "elasticloadbalancing:DescribeListeners",
      "elasticloadbalancing:DescribeListenerCertificates",
      "elasticloadbalancing:DescribeSSLPolicies",
      "elasticloadbalancing:DescribeRules",
      "elasticloadbalancing:DescribeTargetGroups",
      "elasticloadbalancing:DescribeTargetGroupAttributes",
      "elasticloadbalancing:DescribeTargetHealth",
      "elasticloadbalancing:DescribeTags",
      "cognito-idp:DescribeUserPoolClient",
      "acm:ListCertificates",
      "acm:DescribeCertificate",
      "iam:ListServerCertificates",
      "iam:GetServerCertificate",
      "waf-regional:GetWebACL",
      "waf-regional:GetWebACLForResource",
      "waf-regional:AssociateWebACL",
      "waf-regional:DisassociateWebACL",
      "wafv2:GetWebACL",
      "wafv2:GetWebACLForResource",
      "wafv2:AssociateWebACL",
      "wafv2:DisassociateWebACL",
      "shield:GetSubscriptionState",
      "shield:DescribeProtection",
      "shield:CreateProtection",
      "shield:DeleteProtection"
    ]
    resources = ["*"]
  }

  statement {
    actions = [
      "ec2:AuthorizeSecurityGroupIngress",
      "ec2:RevokeSecurityGroupIngress"
    ]
    resources = ["*"]
  }

  statement {
    actions = [
      "ec2:CreateSecurityGroup",
      "ec2:CreateTags"
    ]
    resources = ["*"]
  }

  statement {
    actions = [
      "ec2:DeleteSecurityGroup",
      "ec2:DeleteTags",
      "elasticloadbalancing:CreateLoadBalancer",
      "elasticloadbalancing:CreateTargetGroup",
      "elasticloadbalancing:CreateListener",
      "elasticloadbalancing:CreateRule",
      "elasticloadbalancing:AddTags",
      "elasticloadbalancing:RemoveTags",
      "elasticloadbalancing:ModifyLoadBalancerAttributes",
      "elasticloadbalancing:SetIpAddressType",
      "elasticloadbalancing:SetSecurityGroups",
      "elasticloadbalancing:SetSubnets",
      "elasticloadbalancing:DeleteLoadBalancer",
      "elasticloadbalancing:ModifyTargetGroup",
      "elasticloadbalancing:ModifyTargetGroupAttributes",
      "elasticloadbalancing:DeleteTargetGroup",
      "elasticloadbalancing:RegisterTargets",
      "elasticloadbalancing:DeregisterTargets",
      "elasticloadbalancing:SetWebAcl",
      "elasticloadbalancing:ModifyListener",
      "elasticloadbalancing:AddListenerCertificates",
      "elasticloadbalancing:RemoveListenerCertificates",
      "elasticloadbalancing:DeleteListener",
      "elasticloadbalancing:ModifyRule",
      "elasticloadbalancing:DeleteRule"
    ]
    resources = ["*"]
  }
}

resource "aws_iam_role_policy" "aws_load_balancer_controller" {
  name   = "${local.name_prefix}-aws-load-balancer-controller"
  role   = aws_iam_role.addon["aws_load_balancer_controller"].id
  policy = data.aws_iam_policy_document.aws_load_balancer_controller.json
}

data "aws_iam_policy_document" "external_secrets" {
  statement {
    actions = [
      "secretsmanager:DescribeSecret",
      "secretsmanager:GetSecretValue"
    ]
    resources = [var.application_secret_arn]
  }
}

resource "aws_iam_role_policy" "external_secrets" {
  name   = "${local.name_prefix}-external-secrets"
  role   = aws_iam_role.addon["external_secrets"].id
  policy = data.aws_iam_policy_document.external_secrets.json
}

data "aws_iam_policy_document" "external_dns" {
  statement {
    actions = [
      "route53:ChangeResourceRecordSets"
    ]
    resources = [var.route53_zone_arn]
  }

  statement {
    actions = [
      "route53:ListHostedZones",
      "route53:ListResourceRecordSets"
    ]
    resources = ["*"]
  }
}

resource "aws_iam_role_policy" "external_dns" {
  name   = "${local.name_prefix}-external-dns"
  role   = aws_iam_role.addon["external_dns"].id
  policy = data.aws_iam_policy_document.external_dns.json
}

data "aws_iam_policy_document" "cluster_autoscaler" {
  statement {
    actions = [
      "autoscaling:DescribeAutoScalingGroups",
      "autoscaling:DescribeAutoScalingInstances",
      "autoscaling:DescribeLaunchConfigurations",
      "autoscaling:DescribeScalingActivities",
      "autoscaling:DescribeTags",
      "ec2:DescribeImages",
      "ec2:DescribeInstanceTypes",
      "ec2:DescribeLaunchTemplateVersions",
      "ec2:GetInstanceTypesFromInstanceRequirements",
      "eks:DescribeNodegroup"
    ]
    resources = ["*"]
  }

  statement {
    actions = [
      "autoscaling:SetDesiredCapacity",
      "autoscaling:TerminateInstanceInAutoScalingGroup"
    ]
    resources = ["*"]
    condition {
      test     = "StringEquals"
      variable = "aws:ResourceTag/k8s.io/cluster-autoscaler/${var.cluster_name}"
      values   = ["owned"]
    }
  }
}

resource "aws_iam_role_policy" "cluster_autoscaler" {
  name   = "${local.name_prefix}-cluster-autoscaler"
  role   = aws_iam_role.addon["cluster_autoscaler"].id
  policy = data.aws_iam_policy_document.cluster_autoscaler.json
}

resource "aws_iam_role_policy_attachment" "fluent_bit" {
  role       = aws_iam_role.addon["fluent_bit"].name
  policy_arn = "arn:aws:iam::aws:policy/CloudWatchAgentServerPolicy"
}
