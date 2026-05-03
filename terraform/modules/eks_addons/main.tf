resource "helm_release" "aws_load_balancer_controller" {
  name       = "aws-load-balancer-controller"
  namespace  = var.aws_load_balancer_controller_namespace
  repository = "https://aws.github.io/eks-charts"
  chart      = "aws-load-balancer-controller"

  set {
    name  = "clusterName"
    value = var.cluster_name
  }

  set {
    name  = "region"
    value = var.aws_region
  }

  set {
    name  = "vpcId"
    value = var.vpc_id
  }

  set {
    name  = "serviceAccount.create"
    value = "true"
  }

  set {
    name  = "serviceAccount.name"
    value = "aws-load-balancer-controller"
  }

  set {
    name  = "serviceAccount.annotations.eks\\.amazonaws\\.com/role-arn"
    value = aws_iam_role.addon["aws_load_balancer_controller"].arn
  }
}

resource "helm_release" "external_secrets" {
  name             = "external-secrets"
  namespace        = var.external_secrets_namespace
  create_namespace = true
  repository       = "https://charts.external-secrets.io"
  chart            = "external-secrets"

  set {
    name  = "installCRDs"
    value = "true"
  }

  set {
    name  = "serviceAccount.annotations.eks\\.amazonaws\\.com/role-arn"
    value = aws_iam_role.addon["external_secrets"].arn
  }
}

resource "helm_release" "external_dns" {
  name             = "external-dns"
  namespace        = var.external_dns_namespace
  create_namespace = true
  repository       = "https://kubernetes-sigs.github.io/external-dns/"
  chart            = "external-dns"

  set {
    name  = "provider.name"
    value = "aws"
  }

  set {
    name  = "policy"
    value = "sync"
  }

  set {
    name  = "sources[0]"
    value = "ingress"
  }

  set {
    name  = "serviceAccount.annotations.eks\\.amazonaws\\.com/role-arn"
    value = aws_iam_role.addon["external_dns"].arn
  }
}

resource "helm_release" "metrics_server" {
  name             = "metrics-server"
  namespace        = "kube-system"
  repository       = "https://kubernetes-sigs.github.io/metrics-server/"
  chart            = "metrics-server"
  create_namespace = false
}

resource "helm_release" "cluster_autoscaler" {
  name       = "cluster-autoscaler"
  namespace  = "kube-system"
  repository = "https://kubernetes.github.io/autoscaler"
  chart      = "cluster-autoscaler"

  set {
    name  = "autoDiscovery.clusterName"
    value = var.cluster_name
  }

  set {
    name  = "awsRegion"
    value = var.aws_region
  }

  set {
    name  = "rbac.serviceAccount.name"
    value = "cluster-autoscaler"
  }

  set {
    name  = "rbac.serviceAccount.annotations.eks\\.amazonaws\\.com/role-arn"
    value = aws_iam_role.addon["cluster_autoscaler"].arn
  }

  set {
    name  = "extraArgs.balance-similar-node-groups"
    value = "true"
  }

  set {
    name  = "extraArgs.skip-nodes-with-system-pods"
    value = "false"
  }
}

resource "helm_release" "aws_for_fluent_bit" {
  name             = "aws-for-fluent-bit"
  namespace        = var.observability_namespace
  create_namespace = true
  repository       = "https://aws.github.io/eks-charts"
  chart            = "aws-for-fluent-bit"

  set {
    name  = "cloudWatch.region"
    value = var.aws_region
  }

  set {
    name  = "cloudWatch.logGroupName"
    value = "/aws/eks/${var.cluster_name}/application"
  }

  set {
    name  = "cloudWatch.autoCreateGroup"
    value = "true"
  }

  set {
    name  = "serviceAccount.name"
    value = "aws-for-fluent-bit"
  }

  set {
    name  = "serviceAccount.annotations.eks\\.amazonaws\\.com/role-arn"
    value = aws_iam_role.addon["fluent_bit"].arn
  }
}
