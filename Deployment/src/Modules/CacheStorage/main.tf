resource "azurerm_storage_account" "ess_cache_storage" {
  name = lower("${var.name}")
  resource_group_name = var.resource_group_name
  location = var.location
  account_tier = "Standard"
  account_replication_type = "LRS"
  account_kind = "StorageV2"
  min_tls_version = "TLS1_2"
  allow_nested_items_to_be_public  = false
  network_rules {
    default_action             = "Deny"
    ip_rules                   = var.allowed_ips
    bypass                     = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids = flatten([[var.m_spoke_subnet, var.agent_2204_subnet, var.agent_prd_subnet],var.small_exchange_set_subnets,var.medium_exchange_set_subnets,var.large_exchange_set_subnets])
   }

  tags = var.tags
}
