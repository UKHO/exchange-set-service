variable "apim_name" {
  type    = string
}

variable "apim_rg" {
  type    = string
}

variable "backend_url" {
  type    = string
}

variable "group_name" {
    type = string
    default = "Exchange Set Service"
}

variable "product_name" {
    type = string
    default = "Exchange Set Service"
}

variable "product_description" {
    type = string
    default = "This is exchange set api service product "
}

variable "api_name" {
    type = string
    default = "ess api"
}

variable "api_description" {
    type = string
    default = "This is exchange set api service api "
}

locals {
  env_name				= lower(terraform.workspace)
  service_name			= "ess"
  tags = {
    SERVICE          = "Exchange Set Service"
    ENVIRONMENT      = local.env_name
    SERVICE_OWNER    = "UKHO"
    RESPONSIBLE_TEAM = "Mastek"
    CALLOUT_TEAM     = "On-Call_N/A"
    COST_CENTRE      = "A.008.02"
  }
  apim_api_openapi = file("${path.module}/exchangeSetService_OpenApi_definition.yaml")
}