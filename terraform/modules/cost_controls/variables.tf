variable "project_name" {
  description = "Project name for resource naming"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "monthly_budget_limit_usd" {
  description = "Monthly AWS budget limit in USD"
  type        = number
  default     = 250
}

variable "budget_alert_email" {
  description = "Email address for budget and cost anomaly alerts"
  type        = string
  default     = ""
}

variable "cost_anomaly_threshold_usd" {
  description = "Minimum anomaly impact in USD before sending alerts"
  type        = number
  default     = 25
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
