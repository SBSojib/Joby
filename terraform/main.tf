resource "random_id" "suffix" {
  byte_length = 4
}

locals {
  s3_bucket_name = var.s3_bucket_name != "" ? var.s3_bucket_name : "${var.project_name}-${var.environment}-uploads-${random_id.suffix.hex}"

  app_hostname = var.app_subdomain == "" ? trimsuffix(var.domain_name, ".") : "${var.app_subdomain}.${trimsuffix(var.domain_name, ".")}"

  route53_hosted_zone_name = var.dns_delegation_mode == "subdomain" ? local.app_hostname : trimsuffix(var.domain_name, ".")

  common_tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

resource "aws_cloudwatch_log_group" "app" {
  name              = "/${var.project_name}/${var.environment}"
  retention_in_days = 7
  tags              = local.common_tags
}

module "network" {
  source = "./modules/network"

  project_name            = var.project_name
  environment             = var.environment
  vpc_cidr                = var.vpc_cidr
  public_subnet_cidrs     = var.public_subnet_cidrs
  private_subnet_cidrs    = var.private_subnet_cidrs
  kubernetes_cluster_name = "${var.project_name}-${var.environment}-eks"
  tags                    = local.common_tags
}

module "edge" {
  source = "./modules/edge"

  project_name             = var.project_name
  environment              = var.environment
  domain_name              = var.domain_name
  app_subdomain            = var.app_subdomain
  route53_hosted_zone_name = local.route53_hosted_zone_name
  tags                     = local.common_tags
}

module "eks" {
  source = "./modules/eks"

  project_name                   = var.project_name
  environment                    = var.environment
  cluster_version                = var.eks_cluster_version
  subnet_ids                     = module.network.private_subnet_ids
  vpc_id                         = module.network.vpc_id
  node_instance_types            = var.eks_node_instance_types
  node_desired_size              = var.eks_node_desired_size
  node_min_size                  = var.eks_node_min_size
  node_max_size                  = var.eks_node_max_size
  cluster_endpoint_public_access = var.eks_cluster_endpoint_public_access
  cluster_public_access_cidrs    = var.eks_cluster_public_access_cidrs
  tags                           = local.common_tags
}

module "security" {
  source = "./modules/security"

  project_name = var.project_name
  environment  = var.environment
  vpc_id       = module.network.vpc_id
  tags         = local.common_tags
}

resource "aws_vpc_security_group_ingress_rule" "rds_from_eks_nodes" {
  security_group_id            = module.security.rds_security_group_id
  referenced_security_group_id = module.eks.node_security_group_id
  ip_protocol                  = "tcp"
  from_port                    = 5432
  to_port                      = 5432
  description                  = "PostgreSQL from EKS worker nodes"
}

module "s3" {
  source = "./modules/s3"

  bucket_name = local.s3_bucket_name
  tags        = local.common_tags
}

module "ecr" {
  source = "./modules/ecr"

  project_name = var.project_name
  environment  = var.environment
  tags         = local.common_tags
}

module "backend_irsa" {
  source = "./modules/irsa"

  project_name         = var.project_name
  environment          = var.environment
  oidc_provider_arn    = module.eks.oidc_provider_arn
  oidc_provider_url    = module.eks.cluster_oidc_issuer_url
  namespace            = var.k8s_namespace
  service_account_name = var.backend_service_account_name
  s3_bucket_arn        = module.s3.bucket_arn
  tags                 = local.common_tags
}

module "rds" {
  source = "./modules/rds"

  project_name            = var.project_name
  environment             = var.environment
  subnet_ids              = module.network.private_subnet_ids
  security_group_id       = module.security.rds_security_group_id
  db_name                 = var.db_name
  db_username             = var.db_username
  db_password             = var.db_password
  instance_class          = var.db_instance_class
  allocated_storage       = var.db_allocated_storage
  max_allocated_storage   = var.db_max_allocated_storage
  backup_retention_period = var.db_backup_retention_period
  tags                    = local.common_tags
}

module "secrets" {
  source = "./modules/secrets"

  project_name = var.project_name
  environment  = var.environment
  tags         = local.common_tags
}

module "eks_addons" {
  source = "./modules/eks_addons"

  project_name           = var.project_name
  environment            = var.environment
  aws_region             = var.aws_region
  cluster_name           = module.eks.cluster_name
  vpc_id                 = module.network.vpc_id
  oidc_provider_arn      = module.eks.oidc_provider_arn
  oidc_provider_url      = module.eks.cluster_oidc_issuer_url
  route53_zone_arn       = module.edge.hosted_zone_arn
  route53_zone_id        = module.edge.hosted_zone_id
  route53_domain_filter  = module.edge.hosted_zone_name
  application_secret_arn = module.secrets.application_secret_arn
  tags                   = local.common_tags

  depends_on = [
    module.eks
  ]
}

module "monitoring" {
  source = "./modules/monitoring"

  project_name               = var.project_name
  environment                = var.environment
  aws_region                 = var.aws_region
  rds_instance_identifier    = module.rds.identifier
  eks_cluster_name           = module.eks.cluster_name
  application_log_group_name = aws_cloudwatch_log_group.app.name
  waf_web_acl_name           = module.edge.waf_web_acl_name
  alert_email                = var.monitoring_alert_email
  tags                       = local.common_tags
}

module "cost_controls" {
  source = "./modules/cost_controls"

  project_name               = var.project_name
  environment                = var.environment
  monthly_budget_limit_usd   = var.monthly_budget_limit_usd
  budget_alert_email         = var.billing_alert_email != "" ? var.billing_alert_email : var.monitoring_alert_email
  cost_anomaly_threshold_usd = var.cost_anomaly_threshold_usd
  tags                       = local.common_tags
}

module "security_baseline" {
  source = "./modules/security_baseline"

  project_name = var.project_name
  environment  = var.environment
  tags         = local.common_tags
}
