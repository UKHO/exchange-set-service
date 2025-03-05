variable "location" {
  type    = string
  default = "uksouth"
}

variable "resource_group_name" {
  type    = string
  default = "ess"
}

locals {
  env_name			    = lower(terraform.workspace)
  service_name			= "ess"
  web_app_name		    = "${local.service_name}-${local.env_name}-webapp${var.suffix}"
  key_vault_name		= "${local.service_name}-ukho-${local.env_name}-kv${var.suffix}"
  tags = {
    SERVICE          = "Exchange Set Service"
    ENVIRONMENT      = local.env_name
    SERVICE_OWNER    = "Robin Chapman"
    RESPONSIBLE_TEAM = "Abzu"
    CALLOUT_TEAM     = "On-Call_N/A"
    COST_CENTRE      = local.env_name == "dev" || local.env_name == "qa" || local.env_name == "prod" ? "A.008.02" : "A.011.08"
  }
  config_data = jsondecode(file("${path.module}/appsettings.json"))
  asp_name_sxs = [for i in range(local.config_data.ESSFulfilmentConfiguration.SmallExchangeSetInstance) : "${local.service_name}-${local.env_name}-sxs-${sum([1, i])}${var.asp_control_sxs.zoneRedundant ? "-z" : ""}-asp${var.suffix}"]
  asp_name_mxs = [for i in range(local.config_data.ESSFulfilmentConfiguration.MediumExchangeSetInstance) : "${local.service_name}-${local.env_name}-mxs-${sum([1, i])}${var.asp_control_mxs.zoneRedundant ? "-z" : ""}-asp${var.suffix}"]
  asp_name_lxs = [for i in range(local.config_data.ESSFulfilmentConfiguration.LargeExchangeSetInstance) : "${local.service_name}-${local.env_name}-lxs-${sum([1, i])}${var.asp_control_lxs.zoneRedundant ? "-z" : ""}-asp${var.suffix}"]
  as_name_sxs = [for i in range(local.config_data.ESSFulfilmentConfiguration.SmallExchangeSetInstance) : "${local.service_name}-${local.env_name}-sxs-${sum([1, i])}${var.asp_control_sxs.zoneRedundant ? "-z" : ""}-webapp${var.suffix}"]
  as_name_mxs = [for i in range(local.config_data.ESSFulfilmentConfiguration.MediumExchangeSetInstance) : "${local.service_name}-${local.env_name}-mxs-${sum([1, i])}${var.asp_control_mxs.zoneRedundant ? "-z" : ""}-webapp${var.suffix}"]
  as_name_lxs = [for i in range(local.config_data.ESSFulfilmentConfiguration.LargeExchangeSetInstance) : "${local.service_name}-${local.env_name}-lxs-${sum([1, i])}${var.asp_control_lxs.zoneRedundant ? "-z" : ""}-webapp${var.suffix}"]
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

variable "asp_control_sxs" {
  type = object({ sku = string, zoneRedundant = bool, enableAutoScale = bool, autoScaleMaxInstances = number, autoScaleInThreshold = number, autoScaleOutThreshold = number, autoScaleInAmount = number, autoScaleOutAmount = number })
}

variable "asp_control_mxs" {
  type = object({ sku = string, zoneRedundant = bool, enableAutoScale = bool, autoScaleMaxInstances = number, autoScaleInThreshold = number, autoScaleOutThreshold = number, autoScaleInAmount = number, autoScaleOutAmount = number })
}

variable "asp_control_lxs" {
  type = object({ sku = string, zoneRedundant = bool, enableAutoScale = bool, autoScaleMaxInstances = number, autoScaleInThreshold = number, autoScaleOutThreshold = number, autoScaleInAmount = number, autoScaleOutAmount = number })
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
    "vne"    = {
        tier = "PremiumV3"
        size = "P1v3"
        }
    "vni"    = {
        tier = "PremiumV3"
        size = "P1v3"
        }
    "iat"    = {
        tier = "PremiumV3"
        size = "P1v3"
        }
    "pre"    = {
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

variable "elastic_apm_server_url" {
}

variable "elastic_apm_api_key" {
}

variable "suffix" {
  default     = ""
}

variable "storage_suffix" {
  default     = ""
}

variable "agent_2204_subnet" {
  type = string
}

variable "agent_prd_subnet" {
  type = string
}
