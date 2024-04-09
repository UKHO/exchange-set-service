variable "apim_name" {
  type    = string
}

variable "apim_rg" {
  type    = string
}

variable "apim_api_backend_url" {
  type    = string
}

variable "group_name" {
    type = string
    default = "Exchange Set Service"
}

variable "group_description" {
    type = string
    default = "Management group for users with access to the Exchange Set Service."
}

variable "product_name" {
    type = string
    default = "Exchange Set Service"
}

variable "product_description" {
    type = string
    default = "The Exchange Set Service provides APIs to enable the request of ENC Exchange Sets for loading onto an ECDIS."
}

variable "api_name" {
    type = string
    default = "Exchange Set Service API"
}

variable "api_description" {
    type = string
    default = "The Exchange Set Service APIs to request ENC Exchange Sets for loading onto an ECDIS."
}

variable "ui_product_name" {
    type = string
    default = "Exchange Set Service UI"
}

variable "b2c_token_issuer" {
  type  = string
}

variable "b2c_client_id" {
  type  = string
}

variable "env_suffix" {
  type = map(string)
  default = {
    "dev"     = "Dev"
    "qa"      = "QA"
    "vne"     = "VNE"
    "vni"     = "VNI"
  }
}

variable "product_rate_limit" {
  type = map(any)
  default = {
	    calls = 5
	    renewal-period = 5
    }
}

variable "product_quota" {
  type = map(any)
  default = {
	    calls = 5000
	    renewal-period = 86400
    }
}

variable "ess_ui_product_call_limit" {
  type    = number  
  default = 5
}

variable "ess_ui_product_call_renewal_period" {
  type    = number  
  default = 60
}

variable "ess_ui_product_daily_quota_limit" {
  type    = number 
  default = 100
}

variable "client_credentials_tenant_id" {
	type = string
}

variable "client_credentials_scope" {
	type = string
}

variable "client_credentials_operation_id" {
    type = string
    default = "getESSTokenUsingClientCredentials"
}

variable "cors_origin_values" {
  type = string  
}

variable "policy_rewrite_from_gateway" {
  type = string  
}

variable "policy_rewrite_to_gateway" {
  type = string  
}

locals {
  env_name				= lower(terraform.workspace)
  service_name			= "ess"
  group_name            = local.env_name == "prod" ? var.group_name : "${var.group_name} ${var.env_suffix[local.env_name]}"
  product_name          = local.env_name == "prod" ? var.product_name : "${var.product_name} ${var.env_suffix[local.env_name]}"
  ui_product_name       = local.env_name == "prod" ? var.ui_product_name : "${var.ui_product_name} ${var.env_suffix[local.env_name]}"
  api_name              = local.env_name == "prod" ? var.api_name : "${var.api_name} ${var.env_suffix[local.env_name]}"
  apim_api_path         = local.env_name == "prod" ? local.service_name : "${local.service_name}-${local.env_name}"
  apim_api_openapi      = file("${path.module}/exchangeSetService_OpenApi_definition.yaml")
  cors_origins          = split(";", var.cors_origin_values)
}