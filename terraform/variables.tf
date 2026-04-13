# -----------------------------------------------------------------------------
# General
# -----------------------------------------------------------------------------

variable "aws_region" {
  description = "AWS region to deploy into"
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Project name used for resource naming and tags"
  type        = string
  default     = "joby"

  validation {
    condition     = can(regex("^[a-z][a-z0-9-]{1,20}$", var.project_name))
    error_message = "Must be lowercase alphanumeric with hyphens, 2-21 chars, starting with a letter."
  }
}

variable "environment" {
  description = "Deployment environment (dev, staging, prod)"
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Must be one of: dev, staging, prod."
  }
}

# -----------------------------------------------------------------------------
# EC2
# -----------------------------------------------------------------------------

variable "key_pair_name" {
  description = "Name of an existing EC2 key pair for SSH access"
  type        = string
}

variable "allowed_ssh_cidrs" {
  description = "CIDR blocks allowed to SSH into the EC2 instance (e.g. [\"203.0.113.5/32\"])"
  type        = list(string)
  default     = []

  validation {
    condition     = alltrue([for cidr in var.allowed_ssh_cidrs : can(cidrhost(cidr, 0))])
    error_message = "Every entry must be a valid CIDR block."
  }
}

variable "ec2_instance_type" {
  description = "EC2 instance type (t2.micro and t3.micro are free-tier eligible)"
  type        = string
  default     = "t3.micro"
}

variable "ec2_volume_size" {
  description = "Root EBS volume size in GB"
  type        = number
  default     = 20

  validation {
    condition     = var.ec2_volume_size >= 8 && var.ec2_volume_size <= 100
    error_message = "Must be between 8 and 100 GB."
  }
}

variable "enable_elastic_ip" {
  description = "Allocate an Elastic IP for the EC2 instance"
  type        = bool
  default     = false
}

# -----------------------------------------------------------------------------
# RDS
# -----------------------------------------------------------------------------

variable "db_name" {
  description = "PostgreSQL database name"
  type        = string
  default     = "joby"

  validation {
    condition     = can(regex("^[a-zA-Z][a-zA-Z0-9_]{0,62}$", var.db_name))
    error_message = "Must start with a letter and contain only alphanumeric characters or underscores."
  }
}

variable "db_username" {
  description = "PostgreSQL master username"
  type        = string
  default     = "joby_admin"

  validation {
    condition     = can(regex("^[a-zA-Z][a-zA-Z0-9_]{2,30}$", var.db_username))
    error_message = "Must start with a letter, 3-31 chars, alphanumeric or underscores."
  }
}

variable "db_password" {
  description = "PostgreSQL master password (min 12 characters, keep this secret)"
  type        = string
  sensitive   = true

  validation {
    condition     = length(var.db_password) >= 12
    error_message = "Must be at least 12 characters."
  }
}

variable "db_instance_class" {
  description = "RDS instance class (db.t3.micro is free-tier eligible)"
  type        = string
  default     = "db.t3.micro"
}

variable "db_allocated_storage" {
  description = "RDS allocated storage in GB (free tier includes up to 20 GB)"
  type        = number
  default     = 20
}

variable "db_skip_final_snapshot" {
  description = "Skip final DB snapshot on deletion (true for dev, false for prod)"
  type        = bool
  default     = true
}

variable "db_deletion_protection" {
  description = "Enable deletion protection on the RDS instance"
  type        = bool
  default     = false
}

variable "db_backup_retention_period" {
  description = "Number of days to retain automated backups (0 disables)"
  type        = number
  default     = 1

  validation {
    condition     = var.db_backup_retention_period >= 0 && var.db_backup_retention_period <= 35
    error_message = "Must be between 0 and 35."
  }
}

# -----------------------------------------------------------------------------
# S3
# -----------------------------------------------------------------------------

variable "s3_bucket_name" {
  description = "S3 bucket name for file uploads. Leave empty to auto-generate a unique name."
  type        = string
  default     = ""
}

variable "s3_versioning_enabled" {
  description = "Enable versioning on the S3 bucket"
  type        = bool
  default     = false
}

variable "app_target_port" {
  description = "TCP port on the EC2 security group for inbound app traffic (Docker publish port for the frontend, e.g. 8080)."
  type        = number
  default     = 8080
}
