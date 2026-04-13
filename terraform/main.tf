# -----------------------------------------------------------------------------
# Data sources – default VPC and subnets
# Using the default VPC keeps the setup simple and free-tier friendly.
# For production, consider creating a custom VPC with private subnets.
# -----------------------------------------------------------------------------

data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }

  filter {
    name   = "default-for-az"
    values = ["true"]
  }
}

# -----------------------------------------------------------------------------
# Random suffix for globally unique S3 bucket name
# -----------------------------------------------------------------------------

resource "random_id" "suffix" {
  byte_length = 4
}

# -----------------------------------------------------------------------------
# Locals
# -----------------------------------------------------------------------------

locals {
  s3_bucket_name = var.s3_bucket_name != "" ? var.s3_bucket_name : "${var.project_name}-${var.environment}-uploads-${random_id.suffix.hex}"

  common_tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

# -----------------------------------------------------------------------------
# CloudWatch Log Group
# -----------------------------------------------------------------------------

resource "aws_cloudwatch_log_group" "app" {
  name              = "/${var.project_name}/${var.environment}"
  retention_in_days = 7
  tags              = local.common_tags
}

# -----------------------------------------------------------------------------
# Modules
# -----------------------------------------------------------------------------

module "security" {
  source = "./modules/security"

  project_name      = var.project_name
  environment       = var.environment
  vpc_id            = data.aws_vpc.default.id
  allowed_ssh_cidrs = var.allowed_ssh_cidrs
  app_target_port   = var.app_target_port
  tags              = local.common_tags
}

module "s3" {
  source = "./modules/s3"

  bucket_name        = local.s3_bucket_name
  versioning_enabled = var.s3_versioning_enabled
  tags               = local.common_tags
}

module "iam" {
  source = "./modules/iam"

  project_name            = var.project_name
  environment             = var.environment
  s3_bucket_arn           = module.s3.bucket_arn
  cloudwatch_log_group_arn = aws_cloudwatch_log_group.app.arn
  tags                    = local.common_tags
}

module "rds" {
  source = "./modules/rds"

  project_name            = var.project_name
  environment             = var.environment
  subnet_ids              = data.aws_subnets.default.ids
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

module "ec2" {
  source = "./modules/ec2"

  project_name         = var.project_name
  environment          = var.environment
  instance_type        = var.ec2_instance_type
  volume_size          = var.ec2_volume_size
  key_pair_name        = var.key_pair_name
  security_group_id    = module.security.ec2_security_group_id
  iam_instance_profile = module.iam.instance_profile_name
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
