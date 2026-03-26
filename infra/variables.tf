variable "region" {
  description = "AWS region for all resources"
  type        = string
  default     = "eu-west-1"
}

variable "project_name" {
  description = "Project name used for resource naming and tagging"
  type        = string
  default     = "saa-c03-practice"
}

variable "environment" {
  description = "Environment tag value"
  type        = string
  default     = "dev"
}

variable "google_client_id" {
  description = <<-EOT
    Google OAuth 2.0 Client ID.
    Leave empty on first terraform apply.
    After apply, get the Cognito domain from outputs, create a Google OAuth client,
    then set this value in terraform.tfvars and run terraform apply again.
    Can also be set via the TF_VAR_google_client_id environment variable.
  EOT
  type      = string
  default   = ""
  sensitive = true
}

variable "google_client_secret" {
  description = <<-EOT
    Google OAuth 2.0 Client Secret.
    Set alongside google_client_id on the second terraform apply.
    Can also be set via the TF_VAR_google_client_secret environment variable.
  EOT
  type      = string
  default   = ""
  sensitive = true
}
