variable "project_name" {
  description = "Project name for resource naming"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID to create security groups in"
  type        = string
}

variable "allowed_ssh_cidrs" {
  description = "CIDR blocks allowed to SSH into EC2"
  type        = list(string)
  default     = []
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}

variable "app_target_port" {
  description = "Inbound TCP port on EC2 for the app (e.g. Docker publish port for frontend nginx, 8080:80)"
  type        = number
  default     = 8080
}

variable "additional_rds_ingress_security_group_ids" {
  description = "Additional security groups allowed to access PostgreSQL on RDS"
  type        = list(string)
  default     = []
}
