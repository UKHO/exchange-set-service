variable "apim_service_name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "env_name" {
  type = string
}

variable "apim_api_path" {
  type = string
}

variable "apim_api_service_url" {
  type        = string
  description = "The URL of the backend service serving the API."
}

variable "apim_group_name" {
  type = string
}

variable "apim_ess_product_name" {
  type = string
}

variable "apim_ess_product_description" {
  type = string
}

variable "apim_api_name" {
  type = string
}

variable "apim_api_description" {
  type = string
}

variable "apim_api_openapi" {
  type = string
}
