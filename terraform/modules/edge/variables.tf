variable "project_name" {
  description = "Project name for resource naming"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "domain_name" {
  description = "Root domain name for the public Route 53 hosted zone"
  type        = string
}

variable "app_subdomain" {
  description = "Subdomain used by the public application"
  type        = string
  default     = "app"
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
