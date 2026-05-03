resource "random_id" "suffix" {
  byte_length = 4
}

locals {
  s3_bucket_name = var.s3_bucket_name != "" ? var.s3_bucket_name : "${var.project_name}-${var.environment}-uploads-${random_id.suffix.hex}"
  enable_ec2     = var.provision_ec2

  common_tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

module "network" {
  source = "./modules/network"

  project_name            = var.project_name
  environment             = var.environment
  vpc_cidr                = var.vpc_cidr
  az_count                = var.az_count
  public_subnet_cidrs     = var.public_subnet_cidrs
  private_subnet_cidrs    = var.private_subnet_cidrs
  enable_nat_gateway      = var.enable_nat_gateway
  single_nat_gateway      = var.single_nat_gateway
  enable_vpc_endpoints    = var.enable_vpc_endpoints
  kubernetes_cluster_name = "${var.project_name}-${var.environment}-eks"
  tags                    = local.common_tags
}

module "logging" {
  source = "./modules/logging"

  project_name = var.project_name
  environment  = var.environment
  tags         = local.common_tags
}

module "edge" {
  source = "./modules/edge"

  project_name   = var.project_name
  environment    = var.environment
  domain_name    = var.domain_name
  app_subdomain  = var.app_subdomain
  waf_rate_limit = var.waf_rate_limit
  tags           = local.common_tags
}

module "eks" {
  source = "./modules/eks"

  project_name                    = var.project_name
  environment                     = var.environment
  cluster_version                 = var.eks_cluster_version
  subnet_ids                      = module.network.subnet_ids
  vpc_id                          = module.network.vpc_id
  node_instance_types             = var.eks_node_instance_types
  node_desired_size               = var.eks_node_desired_size
  node_min_size                   = var.eks_node_min_size
  node_max_size                   = var.eks_node_max_size
  cluster_endpoint_public_access  = var.eks_cluster_endpoint_public_access
  cluster_endpoint_private_access = var.eks_cluster_endpoint_private_access
  cluster_public_access_cidrs     = var.eks_cluster_public_access_cidrs
  tags                            = local.common_tags
}

module "security" {
  source = "./modules/security"

  project_name      = var.project_name
  environment       = var.environment
  vpc_id            = module.network.vpc_id
  allowed_ssh_cidrs = var.allowed_ssh_cidrs
  app_target_port   = var.app_target_port
  tags              = local.common_tags
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

  bucket_name        = local.s3_bucket_name
  versioning_enabled = var.s3_versioning_enabled
  force_destroy      = var.s3_force_destroy
  tags               = local.common_tags
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

module "iam" {
  count  = local.enable_ec2 ? 1 : 0
  source = "./modules/iam"

  project_name             = var.project_name
  environment              = var.environment
  s3_bucket_arn            = module.s3.bucket_arn
  cloudwatch_log_group_arn = module.logging.log_group_arn
  tags                     = local.common_tags
}

module "rds" {
  source = "./modules/rds"

  project_name                 = var.project_name
  environment                  = var.environment
  subnet_ids                   = module.network.subnet_ids
  security_group_id            = module.security.rds_security_group_id
  db_name                      = var.db_name
  db_username                  = var.db_username
  db_password                  = var.db_password
  instance_class               = var.db_instance_class
  allocated_storage            = var.db_allocated_storage
  max_allocated_storage        = var.db_max_allocated_storage
  multi_az                     = var.db_multi_az
  performance_insights_enabled = var.db_performance_insights_enabled
  skip_final_snapshot          = var.db_skip_final_snapshot
  deletion_protection          = var.db_deletion_protection
  backup_retention_period      = var.db_backup_retention_period
  monitoring_interval          = var.db_monitoring_interval
  tags                         = local.common_tags
}

module "secrets" {
  source = "./modules/secrets"

  project_name        = var.project_name
  environment         = var.environment
  db_host             = module.rds.address
  db_port             = module.rds.port
  db_name             = var.db_name
  db_username         = var.db_username
  db_password         = var.db_password
  jwt_secret          = var.jwt_secret
  manage_secret_value = var.manage_application_secret_value
  tags                = local.common_tags
}

module "eks_addons" {
  count  = var.enable_kubernetes_addons ? 1 : 0
  source = "./modules/eks_addons"

  project_name           = var.project_name
  environment            = var.environment
  aws_region             = var.aws_region
  cluster_name           = module.eks.cluster_name
  vpc_id                 = module.network.vpc_id
  oidc_provider_arn      = module.eks.oidc_provider_arn
  oidc_provider_url      = module.eks.cluster_oidc_issuer_url
  route53_zone_arn       = module.edge.hosted_zone_arn
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
  application_log_group_name = module.logging.log_group_name
  alert_email                = var.monitoring_alert_email
  enable_alerting            = var.enable_monitoring_alerting
  tags                       = local.common_tags
}

module "security_baseline" {
  count  = var.enable_security_baseline ? 1 : 0
  source = "./modules/security_baseline"

  project_name = var.project_name
  environment  = var.environment
  tags         = local.common_tags
}

module "ec2" {
  count  = local.enable_ec2 ? 1 : 0
  source = "./modules/ec2"

  project_name         = var.project_name
  environment          = var.environment
  instance_type        = var.ec2_instance_type
  subnet_id            = module.network.public_subnet_ids[0]
  volume_size          = var.ec2_volume_size
  key_pair_name        = var.key_pair_name
  security_group_id    = module.security.ec2_security_group_id
  iam_instance_profile = module.iam[0].instance_profile_name
  enable_elastic_ip    = var.enable_elastic_ip
  tags                 = local.common_tags

  user_data_vars = {
    project_name   = var.project_name
    aws_region     = var.aws_region
    db_host        = module.rds.address
    db_port        = tostring(module.rds.port)
    db_name        = var.db_name
    db_username    = var.db_username
    s3_bucket_name = module.s3.bucket_name
  }
}
