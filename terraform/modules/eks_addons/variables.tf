variable "project_name" {
  description = "Project name for resource naming"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "aws_region" {
  description = "AWS region"
  type        = string
}

variable "cluster_name" {
  description = "EKS cluster name"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID"
  type        = string
}

variable "oidc_provider_arn" {
  description = "EKS OIDC provider ARN"
  type        = string
}

variable "oidc_provider_url" {
  description = "EKS OIDC issuer URL"
  type        = string
}

variable "route53_zone_arn" {
  description = "Route 53 hosted zone ARN managed by ExternalDNS"
  type        = string
}

variable "application_secret_arn" {
  description = "Secrets Manager secret ARN read by External Secrets Operator"
  type        = string
}

variable "external_secrets_namespace" {
  description = "Namespace for External Secrets Operator"
  type        = string
  default     = "external-secrets"
}

variable "aws_load_balancer_controller_namespace" {
  description = "Namespace for AWS Load Balancer Controller"
  type        = string
  default     = "kube-system"
}

variable "external_dns_namespace" {
  description = "Namespace for ExternalDNS"
  type        = string
  default     = "external-dns"
}

variable "observability_namespace" {
  description = "Namespace for logging add-ons"
  type        = string
  default     = "amazon-cloudwatch"
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
