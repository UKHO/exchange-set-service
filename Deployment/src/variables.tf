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

  tags = {
    SERVICE          = "Exchange Set Service"
    ENVIRONMENT      = local.env_name
    SERVICE_OWNER    = "UKHO"
    RESPONSIBLE_TEAM = "Mastek"
    CALLOUT_TEAM     = "On-Call_N/A"
    COST_CENTRE      = "A.008.02"
  }
}
