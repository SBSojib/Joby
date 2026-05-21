variable "project_name" {
  description = "Project name for resource naming"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "domain_name" {
  description = "Parent domain used to build the application hostname (e.g. hasibul.bd)"
  type        = string
}

variable "app_subdomain" {
  description = "Subdomain used by the public application"
  type        = string
  default     = "app"
}

variable "route53_hosted_zone_name" {
  description = "Route 53 hosted zone to create (e.g. joby.hasibul.bd for subdomain delegation from Cloudflare)"
  type        = string
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
