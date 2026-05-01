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
}

module "logging" {
  source = "./modules/logging"

  project_name = var.project_name
  environment  = var.environment
  tags         = local.common_tags
}

module "eks" {
  source = "./modules/eks"

  project_name                 = var.project_name
  environment                  = var.environment
  cluster_version              = var.eks_cluster_version
  subnet_ids                   = module.network.subnet_ids
  vpc_id                       = module.network.vpc_id
  node_instance_types          = var.eks_node_instance_types
  node_desired_size            = var.eks_node_desired_size
  node_min_size                = var.eks_node_min_size
  node_max_size                = var.eks_node_max_size
  cluster_public_access_cidrs  = var.eks_cluster_public_access_cidrs
  tags                         = local.common_tags
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
  tags               = local.common_tags
}

module "ecr" {
  source = "./modules/ecr"

  project_name = var.project_name
  environment  = var.environment
  tags         = local.common_tags
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

  project_name            = var.project_name
  environment             = var.environment
  subnet_ids              = module.network.subnet_ids
  security_group_id       = module.security.rds_security_group_id
  db_name                 = var.db_name
  db_username             = var.db_username
  db_password             = var.db_password
  instance_class          = var.db_instance_class
  allocated_storage       = var.db_allocated_storage
  skip_final_snapshot     = var.db_skip_final_snapshot
  deletion_protection     = var.db_deletion_protection
  backup_retention_period = var.db_backup_retention_period
  tags                    = local.common_tags
}

module "monitoring" {
  source = "./modules/monitoring"

  project_name                 = var.project_name
  environment                  = var.environment
  aws_region                   = var.aws_region
  rds_instance_identifier      = module.rds.identifier
  eks_cluster_name             = module.eks.cluster_name
  application_log_group_name   = module.logging.log_group_name
  alert_email                  = var.monitoring_alert_email
  enable_alerting              = var.enable_monitoring_alerting
  tags                         = local.common_tags
}

module "ec2" {
  count  = local.enable_ec2 ? 1 : 0
  source = "./modules/ec2"

  project_name         = var.project_name
  environment          = var.environment
  instance_type        = var.ec2_instance_type
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
