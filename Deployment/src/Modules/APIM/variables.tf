variable "apim_name" {
  type    = string
}

variable "resource_group" {
  type    = string
}

variable "backend_url" {
  type    = string
}

variable "product_name" {
    type = string
    default = "Exchange Set Service"
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