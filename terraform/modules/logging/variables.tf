variable "project_name" {
  description = "Project name for log group naming"
  type        = string
}

variable "environment" {
  description = "Environment name for log group naming"
  type        = string
}

variable "retention_in_days" {
  description = "CloudWatch log group retention period in days"
  type        = number
  default     = 7
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
