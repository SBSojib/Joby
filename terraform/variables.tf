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
  description = "Root domain name for the public Route 53 hosted zone"
  type        = string
  default     = "example.com"
}

variable "app_subdomain" {
  description = "Subdomain used by the public Joby application"
  type        = string
  default     = "app"
}

variable "waf_rate_limit" {
  description = "Maximum requests per five-minute period from a single IP before WAF blocks traffic"
  type        = number
  default     = 2000
}

variable "vpc_cidr" {
  description = "CIDR block for the dedicated VPC"
  type        = string
  default     = "10.20.0.0/16"
}

variable "az_count" {
  description = "Number of Availability Zones to use for public and private subnets"
  type        = number
  default     = 3

  validation {
    condition     = var.az_count >= 2 && var.az_count <= 3
    error_message = "Use two or three Availability Zones."
  }
}

variable "public_subnet_cidrs" {
  description = "CIDR blocks for public subnets"
  type        = list(string)
  default     = ["10.20.0.0/24", "10.20.1.0/24", "10.20.2.0/24"]
}

variable "private_subnet_cidrs" {
  description = "CIDR blocks for private application and database subnets"
  type        = list(string)
  default     = ["10.20.10.0/24", "10.20.11.0/24", "10.20.12.0/24"]
}

variable "enable_nat_gateway" {
  description = "Create NAT gateway egress for private subnets"
  type        = bool
  default     = true
}

variable "single_nat_gateway" {
  description = "Use one NAT gateway instead of one per Availability Zone"
  type        = bool
  default     = false
}

variable "enable_vpc_endpoints" {
  description = "Create VPC endpoints for private AWS API access"
  type        = bool
  default     = true
}

variable "key_pair_name" {
  description = "Name of an existing EC2 key pair for SSH access"
  type        = string
  default     = ""

  validation {
    condition     = !var.provision_ec2 || length(trimspace(var.key_pair_name)) > 0
    error_message = "key_pair_name must be set when provision_ec2 is true."
  }
}

variable "provision_ec2" {
  description = "Whether to provision the EC2 host used for Docker Compose deployment"
  type        = bool
  default     = false
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

variable "jwt_secret" {
  description = "JWT signing secret. Required when manage_application_secret_value is true."
  type        = string
  sensitive   = true
  default     = ""
}

variable "manage_application_secret_value" {
  description = "Store application runtime secret values in Secrets Manager through Terraform"
  type        = bool
  default     = false
}

variable "db_instance_class" {
  description = "RDS instance class (db.t3.micro is free-tier eligible)"
  type        = string
  default     = "db.t3.medium"
}

variable "db_allocated_storage" {
  description = "RDS allocated storage in GB (free tier includes up to 20 GB)"
  type        = number
  default     = 50
}

variable "db_max_allocated_storage" {
  description = "Maximum RDS allocated storage in GB for autoscaling"
  type        = number
  default     = 200
}

variable "db_multi_az" {
  description = "Enable synchronous standby in another Availability Zone for RDS"
  type        = bool
  default     = true
}

variable "db_performance_insights_enabled" {
  description = "Enable Performance Insights for RDS query and load troubleshooting"
  type        = bool
  default     = true
}

variable "db_skip_final_snapshot" {
  description = "Skip final DB snapshot on deletion (true for dev, false for prod)"
  type        = bool
  default     = false
}

variable "db_deletion_protection" {
  description = "Enable deletion protection on the RDS instance"
  type        = bool
  default     = true
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

variable "db_monitoring_interval" {
  description = "RDS enhanced monitoring interval in seconds"
  type        = number
  default     = 60
}

variable "s3_bucket_name" {
  description = "S3 bucket name for file uploads. Leave empty to auto-generate a unique name."
  type        = string
  default     = ""
}

variable "s3_versioning_enabled" {
  description = "Enable versioning on the S3 bucket"
  type        = bool
  default     = true
}

variable "s3_force_destroy" {
  description = "Allow Terraform to delete non-empty upload buckets"
  type        = bool
  default     = false
}

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

variable "enable_kubernetes_addons" {
  description = "Install Terraform-managed Helm/Kubernetes add-ons after the EKS cluster is reachable"
  type        = bool
  default     = false
}

variable "app_target_port" {
  description = "TCP port on the EC2 security group for inbound app traffic (Docker publish port for the frontend, e.g. 8080)."
  type        = number
  default     = 8080
}

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

variable "eks_cluster_public_access_cidrs" {
  description = "CIDR ranges allowed to access the EKS API server"
  type        = list(string)
  default     = ["0.0.0.0/0"]
}

variable "eks_cluster_endpoint_public_access" {
  description = "Expose the EKS API endpoint publicly"
  type        = bool
  default     = true
}

variable "eks_cluster_endpoint_private_access" {
  description = "Expose the EKS API endpoint inside the VPC"
  type        = bool
  default     = true
}

variable "monitoring_alert_email" {
  description = "Email address for CloudWatch alarm notifications. Leave empty to skip email subscription."
  type        = string
  default     = ""
}

variable "enable_monitoring_alerting" {
  description = "Enable CloudWatch alarms and SNS alerting resources."
  type        = bool
  default     = true
}

variable "enable_security_baseline" {
  description = "Enable CloudTrail, GuardDuty, AWS Config, and Security Hub"
  type        = bool
  default     = true
}
