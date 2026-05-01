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

variable "tags" {
  type    = map(string)
  default = {}
}
