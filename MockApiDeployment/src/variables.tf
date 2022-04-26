variable "location" {
  type    = string
  default = "uksouth"
}

variable "app_service_sku" {
  type = map(any)
  default = {
    "qc"     = {
	    tier = "PremiumV3"
	    size = "P1v3"
    }
  }
}

locals {
  env_name				= lower(terraform.workspace)
  service_name			= "essft"
  key_vault_name		= "${local.service_name}-${local.env_name}-kv"
  storage_account_name  = "ft${local.env_name}storageukho"
  managed_identity_name = "${local.service_name}-${local.env_name}-service-identity"
  tags = {
    SERVICE          = "test"
    ENVIRONMENT      = "functionaltest-${local.env_name}"
    SERVICE_OWNER    = "Robin Chapman"
    RESPONSIBLE_TEAM = "Abzu"
    CALLOUT_TEAM     = "On-Call_N/A"
    COST_CENTRE      = "P.431"
  }
}