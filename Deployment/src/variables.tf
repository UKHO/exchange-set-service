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
    service          = "Exchange Set Service"
    environment      = local.env_name
    service_owner    = "UKHO"
    responsible_team = "Mastek"
    callout_team     = "On-Call_N/A"
  }
}
