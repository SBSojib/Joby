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

variable "domain_name" {
  description = "Parent domain (e.g. hasibul.bd). Cloudflare remains authoritative when using subdomain delegation."
  type        = string
  default     = "hasibul.bd"
}

variable "app_subdomain" {
  description = "Subdomain used by the public application (empty string uses the parent domain apex only)"
  type        = string
  default     = "joby"
}

variable "dns_delegation_mode" {
  description = "apex: Route 53 hosts the full domain (change registrar NS). subdomain: Route 53 hosts only the app FQDN; delegate via NS records in Cloudflare."
  type        = string
  default     = "subdomain"

  validation {
    condition     = contains(["apex", "subdomain"], var.dns_delegation_mode)
    error_message = "Must be one of: apex, subdomain."
  }
}

# ---------------------------------------------------------------------------
# Network
# ---------------------------------------------------------------------------

variable "vpc_cidr" {
  description = "CIDR block for the dedicated VPC"
  type        = string
  default     = "10.20.0.0/16"
}

variable "public_subnet_cidrs" {
  description = "CIDR blocks for three public subnets (one per AZ)"
  type        = list(string)
  default     = ["10.20.0.0/24", "10.20.1.0/24", "10.20.2.0/24"]

  validation {
    condition     = length(var.public_subnet_cidrs) == 3
    error_message = "Exactly three public subnet CIDR blocks are required."
  }
}

variable "private_subnet_cidrs" {
  description = "CIDR blocks for three private subnets (one per AZ)"
  type        = list(string)
  default     = ["10.20.10.0/24", "10.20.11.0/24", "10.20.12.0/24"]

  validation {
    condition     = length(var.private_subnet_cidrs) == 3
    error_message = "Exactly three private subnet CIDR blocks are required."
  }
}

# ---------------------------------------------------------------------------
# Database
# ---------------------------------------------------------------------------

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
  description = "RDS instance class"
  type        = string
  default     = "db.t3.medium"
}

variable "db_allocated_storage" {
  description = "RDS allocated storage in GB"
  type        = number
  default     = 50
}

variable "db_max_allocated_storage" {
  description = "Maximum RDS allocated storage in GB for autoscaling"
  type        = number
  default     = 200
}

variable "db_backup_retention_period" {
  description = "Number of days to retain automated backups (0 disables)"
  type        = number
  default     = 7

  validation {
    condition     = var.db_backup_retention_period >= 0 && var.db_backup_retention_period <= 35
    error_message = "Must be between 0 and 35."
  }
}

# ---------------------------------------------------------------------------
# S3
# ---------------------------------------------------------------------------

variable "s3_bucket_name" {
  description = "S3 bucket name for file uploads. Leave empty to auto-generate a unique name."
  type        = string
  default     = ""
}

# ---------------------------------------------------------------------------
# EKS
# ---------------------------------------------------------------------------

variable "eks_cluster_version" {
  description = "Kubernetes version for the EKS cluster"
  type        = string
  default     = "1.30"
}

variable "eks_node_instance_types" {
  description = "EC2 instance types used by EKS managed node group"
  type        = list(string)
  default     = ["t3.medium"]
}

variable "eks_node_desired_size" {
  description = "Desired number of worker nodes"
  type        = number
  default     = 2
}

variable "eks_node_min_size" {
  description = "Minimum number of worker nodes"
  type        = number
  default     = 2
}

variable "eks_node_max_size" {
  description = "Maximum number of worker nodes"
  type        = number
  default     = 4
}

variable "eks_cluster_endpoint_public_access" {
  description = "Expose the EKS API endpoint publicly"
  type        = bool
  default     = false
}

variable "eks_cluster_public_access_cidrs" {
  description = "CIDR ranges allowed to access the EKS API server (only applies when public access is enabled)"
  type        = list(string)
  default     = []
}

# ---------------------------------------------------------------------------
# Kubernetes workload identity
# ---------------------------------------------------------------------------

variable "k8s_namespace" {
  description = "Kubernetes namespace used by the application"
  type        = string
  default     = "joby"
}

variable "backend_service_account_name" {
  description = "Kubernetes service account used by the backend workload"
  type        = string
  default     = "joby-backend"
}

# ---------------------------------------------------------------------------
# Monitoring
# ---------------------------------------------------------------------------

variable "monitoring_alert_email" {
  description = "Email address for CloudWatch alarm notifications. Leave empty to skip email subscription."
  type        = string
  default     = ""
}

# ---------------------------------------------------------------------------
# Cost controls
# ---------------------------------------------------------------------------

variable "billing_alert_email" {
  description = "Email address for AWS Budgets and Cost Anomaly Detection alerts"
  type        = string
  default     = ""
}

variable "monthly_budget_limit_usd" {
  description = "Monthly AWS budget limit in USD"
  type        = number
  default     = 250
}

variable "cost_anomaly_threshold_usd" {
  description = "Minimum anomaly impact in USD before sending alerts"
  type        = number
  default     = 25
}
