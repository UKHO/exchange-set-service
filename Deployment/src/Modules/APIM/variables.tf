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

variable "ui_api_name" {
    type = string
    default = "Exchange Set Service UI API"
}

variable "ui_api_description" {
    type = string
    default = "The Exchange Set Service UI api to facilitate ESS UI Application requests."
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

variable "suffix" {
    default = ""
}

variable "pathsuffix" {
    default = ""
}

locals {
  env_name				      = lower(terraform.workspace)
  service_name			    = "ess"
  group_name            = local.env_name == "prod" ? "${var.group_name}${var.suffix}" : "${var.group_name} ${var.env_suffix[local.env_name]}${var.suffix}"
  product_name          = local.env_name == "prod" ? "${var.product_name}${var.suffix}" : "${var.product_name} ${var.env_suffix[local.env_name]}${var.suffix}"
  ui_product_name       = local.env_name == "prod" ? "${var.ui_product_name}${var.suffix}" : "${var.ui_product_name} ${var.env_suffix[local.env_name]}${var.suffix}"
  api_name              = local.env_name == "prod" ? "${var.api_name}${var.suffix}" : "${var.api_name} ${var.env_suffix[local.env_name]}${var.suffix}"
  apim_api_path         = local.env_name == "prod" ? "${local.service_name}${var.pathsuffix}" : "${local.service_name}-${local.env_name}${var.pathsuffix}"
  ui_api_name           = local.env_name == "prod" ? "${var.ui_api_name}${var.suffix}" : "${var.ui_api_name} ${var.env_suffix[local.env_name]}${var.suffix}"
  apim_ui_api_path      = local.env_name == "prod" ? "${local.service_name}-ui${var.suffix}" : "${local.service_name}-ui-${local.env_name}${var.pathsuffix}"

  apim_api_openapi      = file("${path.module}/exchangeSetService_OpenApi_definition.yaml")
  apim_ui_openapi       = file("${path.module}/exchangeSetService_Ui_OpenApi_definition.yaml")

  cors_origins          = split(";", var.cors_origin_values)
}