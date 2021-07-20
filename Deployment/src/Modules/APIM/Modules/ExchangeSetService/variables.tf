variable "apim_name" {
  type = string
}

variable "apim_rg" {
  type = string
}

variable "env_name" {
  type = string
}

variable "apim_api_path" {
  type = string
}

variable "apim_api_backend_url" {
  type        = string
  description = "The URL of the backend service serving the API."
}

variable "apim_group_name" {
  type = string
}

variable "apim_group_description" {
  type = string
}

variable "apim_ess_product_name" {
  type = string
}

variable "apim_ess_product_description" {
  type = string
}

variable "product_rate_limit" {
  type = map(any)
}

variable "product_quota" {
  type = map(any)
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
