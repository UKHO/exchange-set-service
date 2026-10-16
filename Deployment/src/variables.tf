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
    COST_CENTRE      = local.env_name == "dev" || local.env_name == "prod" ? "A.008.02" : "A.011.08"
  }
  config_data = jsondecode(file("${path.module}/appsettings.json"))
  # These names should match those used in AzureWebJobsHealthCheckService in UKHO.ExchangeSetService.Common.
  asp_name_sxs = [for i in range(local.config_data.ESSFulfilmentConfiguration.SmallExchangeSetInstance) : "${local.service_name}-${local.env_name}-sxs-${sum([1, i])}-asp${var.suffix}"]
  asp_name_mxs = [for i in range(local.config_data.ESSFulfilmentConfiguration.MediumExchangeSetInstance) : "${local.service_name}-${local.env_name}-mxs-${sum([1, i])}-asp${var.suffix}"]
  asp_name_lxs = [for i in range(local.config_data.ESSFulfilmentConfiguration.LargeExchangeSetInstance) : "${local.service_name}-${local.env_name}-lxs-${sum([1, i])}-asp${var.suffix}"]
  as_name_sxs = [for i in range(local.config_data.ESSFulfilmentConfiguration.SmallExchangeSetInstance) : "${local.service_name}-${local.env_name}-sxs-${sum([1, i])}-webapp${var.suffix}"]
  as_name_mxs = [for i in range(local.config_data.ESSFulfilmentConfiguration.MediumExchangeSetInstance) : "${local.service_name}-${local.env_name}-mxs-${sum([1, i])}-webapp${var.suffix}"]
  as_name_lxs = [for i in range(local.config_data.ESSFulfilmentConfiguration.LargeExchangeSetInstance) : "${local.service_name}-${local.env_name}-lxs-${sum([1, i])}-webapp${var.suffix}"]
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
  type = object({ sku = string, zoneRedundant = bool, enableAutoScale = bool, autoScaleMinInstances = number, autoScaleMaxInstances = number, autoScaleInThreshold = number, autoScaleOutThreshold = number, autoScaleInAmount = number, autoScaleOutAmount = number })
}

variable "asp_control_mxs" {
  type = object({ sku = string, zoneRedundant = bool, enableAutoScale = bool, autoScaleMinInstances = number, autoScaleMaxInstances = number, autoScaleInThreshold = number, autoScaleOutThreshold = number, autoScaleInAmount = number, autoScaleOutAmount = number })
}

variable "asp_control_lxs" {
  type = object({ sku = string, zoneRedundant = bool, enableAutoScale = bool, autoScaleMinInstances = number, autoScaleMaxInstances = number, autoScaleInThreshold = number, autoScaleOutThreshold = number, autoScaleInAmount = number, autoScaleOutAmount = number })
}

variable "asp_control_webapp" {
  type = object({ sku = string, zoneRedundant = bool })
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

variable "scs_response_retention" {
  type    = number
  default = 7
}

# Front Door; set per environment in config/<workspace>.tfvars.
variable "front_door_enabled" {
  description = "Whether to configure Front Door delivery routing and Front Door access restrictions for the ESS web app."
  type        = bool
  default     = false
}

variable "front_door_route_enabled" {
  description = "Whether the Front Door route is enabled. Allows the route to be disabled without removing resources. Only applies when front_door_enabled is true."
  type        = bool
  default     = true
}

variable "front_door_subscription_id" {
  description = "The subscription ID where the shared Azure Front Door profile is deployed."
  type        = string
  default     = ""
}

variable "front_door_profile_name" {
  description = "The name of the existing shared Azure Front Door profile created by the platform team."
  type        = string
  default     = ""
}

variable "front_door_resource_group" {
  description = "The resource group containing the shared Azure Front Door profile."
  type        = string
  default     = ""
}

variable "front_door_shared_endpoint_name" {
  description = "The name of the shared Front Door endpoint to attach routes to."
  type        = string
  default     = ""
}

variable "front_door_api_custom_domain_hostname" {
  description = "The public hostname for the API instance (suffix \"\"). Leave empty to skip Front Door for this instance."
  type        = string
  default     = ""
}

variable "front_door_v2_custom_domain_hostname" {
  description = "The public hostname for the v2 instance (suffix \"-v2\"), only deployed in route-to-live environments. Leave empty to skip Front Door for this instance."
  type        = string
  default     = ""
}

variable "front_door_custom_domain_dns_zone_id" {
  description = "The resource ID of the Azure DNS zone used for custom domain validation. May be empty while DNS is hosted outside Azure."
  type        = string
  default     = ""
}
