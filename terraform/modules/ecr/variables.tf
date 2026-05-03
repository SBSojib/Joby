variable "project_name" {
  type = string
}

variable "environment" {
  type = string
}

variable "backend_repository_name" {
  type    = string
  default = "joby-backend"
}

variable "frontend_repository_name" {
  type    = string
  default = "joby-frontend"
}

variable "untagged_image_retention_days" {
  type    = number
  default = 14
}

variable "image_tag_mutability" {
  type    = string
  default = "IMMUTABLE"

  validation {
    condition     = contains(["MUTABLE", "IMMUTABLE"], var.image_tag_mutability)
    error_message = "image_tag_mutability must be MUTABLE or IMMUTABLE."
  }
}

variable "tags" {
  type    = map(string)
  default = {}
}
