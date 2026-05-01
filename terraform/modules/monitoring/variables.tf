variable "project_name" {
  type = string
}

variable "environment" {
  type = string
}

variable "aws_region" {
  type = string
}

variable "rds_instance_identifier" {
  type = string
}

variable "eks_cluster_name" {
  type = string
}

variable "application_log_group_name" {
  type = string
}

variable "alert_email" {
  type    = string
  default = ""
}

variable "enable_alerting" {
  type    = bool
  default = true
}

variable "tags" {
  type    = map(string)
  default = {}
}
