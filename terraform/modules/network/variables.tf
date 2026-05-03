variable "project_name" {
  description = "Project name for resource naming"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "vpc_cidr" {
  description = "CIDR block for the dedicated VPC"
  type        = string
  default     = "10.20.0.0/16"
}

variable "az_count" {
  description = "Number of Availability Zones to use"
  type        = number
  default     = 2

  validation {
    condition     = var.az_count >= 2 && var.az_count <= 3
    error_message = "Use two or three Availability Zones for this stack."
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

variable "kubernetes_cluster_name" {
  description = "EKS cluster name used for load balancer subnet discovery tags"
  type        = string
  default     = ""
}

variable "enable_vpc_endpoints" {
  description = "Create VPC endpoints for private AWS API access"
  type        = bool
  default     = true
}

variable "interface_endpoint_services" {
  description = "AWS service short names for interface VPC endpoints"
  type        = list(string)
  default = [
    "ecr.api",
    "ecr.dkr",
    "logs",
    "secretsmanager",
    "ssm",
    "ssmmessages",
    "ec2messages",
    "sts"
  ]
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
