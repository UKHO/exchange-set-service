variable "resource_group_name" {
  type = string
}

variable "apim_service_name" {
  type = string
}

variable "env" {
  type = string
}

variable "EnvSuffix" {
  type = map(string)
  default = {
    "dev" = "Dev"
    "qa" = "QA"
    "prod" = " "
  }
}

variable "apim_group_name" {
  type = string
}

variable "apim_api_path" {
  type = string
}

variable "apim_fss_product_name" {
  type = string
}

variable "apim_fss_product_description" {
  type = string
}

variable "apim_fss_ui_product_name" {
  type = string
}

variable "apim_fss_ui_product_description" {
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

variable "apim_api_service_url" {
  type        = string
  description = "The URL of the backend service serving the API."
}

variable "fss_tennant_id" {
  type    = string
  default = ""
}