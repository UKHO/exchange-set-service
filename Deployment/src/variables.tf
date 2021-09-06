variable "location" {
  type    = string
  default = "uksouth"
}

variable "resource_group_name" {
  type    = string
  default = "ess"
}

locals {
  env_name				= lower(terraform.workspace)
  service_name			= "ess"
  web_app_name		    = "${local.service_name}-${local.env_name}-webapp"
  key_vault_name		= "${local.service_name}-ukho-${local.env_name}-kv"
  tags = {
    SERVICE          = "Exchange Set Service"
    ENVIRONMENT      = local.env_name
    SERVICE_OWNER    = "UKHO"
    RESPONSIBLE_TEAM = "Mastek"
    CALLOUT_TEAM     = "On-Call_N/A"
    COST_CENTRE      = "A.008.02"
  }
  config_data = jsondecode(file("${path.module}/appsettings.json"))
}

variable "allowed_ips" {
  type = list
}

variable "spoke_rg" {
  type = string
}

variable "spoke_vnet_name" {
  type = string
}

variable "spoke_subnet_name" {
  type = string
}

variable "app_service_sku" {
  type = map(any)
  default = {
    "dev"    = {
	    tier = "PremiumV2"
	    size = "P1v2"
        }
    "qa"     = {
	    tier = "PremiumV3"
	    size = "P1v3"
        }
    "prod"   = {
	    tier = "PremiumV3"
	    size = "P1v3"
        }
  }
}

variable "agent_rg" {
  type = string
}

variable "agent_vnet_name" {
  type = string
}

variable "agent_subnet_name" {
  type = string
}

variable "agent_subscription_id" {
  type = string
}