variable "bucket_name" {
  description = "Name of the S3 bucket (must be globally unique)"
  type        = string
}

variable "versioning_enabled" {
  description = "Enable versioning on the S3 bucket"
  type        = bool
  default     = false
}

variable "force_destroy" {
  description = "Allow Terraform to delete non-empty buckets"
  type        = bool
  default     = false
}

variable "noncurrent_version_expiration_days" {
  description = "Days to retain noncurrent object versions"
  type        = number
  default     = 30
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
