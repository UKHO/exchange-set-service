resource "azurerm_storage_account" "small_exchange_set_storage" {
  name = lower("${var.service_name}${var.env_name}sxsstorageukho")
  resource_group_name = var.resource_group_name
  location = var.location
  account_tier = "Standard"
  account_replication_type = "LRS"
  account_kind = "StorageV2"

  network_rules {
    default_action             = "Deny"
    ip_rules                   = var.allowed_ips
    bypass                     = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids = concat(var.small_exchange_set_subnets,[var.m_spoke_subnet])
  }

  tags = var.tags
}

resource "azurerm_storage_container" "small_exchange_set_storage_container" {
  name                  = "ess-fulfilment"
  storage_account_name  = azurerm_storage_account.small_exchange_set_storage.name
}

resource "azurerm_storage_queue" "small_exchange_set_storage_queue" {
  count                = var.exchange_set_config.SmallExchangeSetInstance
  name                 = "ess-sxs-${count.index}-fulfilment"
  storage_account_name = azurerm_storage_account.small_exchange_set_storage.name
}