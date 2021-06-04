resource "azurerm_storage_account" "storage" {
  name = lower(var.name)
  resource_group_name = var.resource_group_name
  location = var.location
  account_tier = "Standard"
  account_replication_type = "LRS"
  account_kind = "StorageV2"

  /*network_rules {
    default_action             = "Deny"
    ip_rules                   = var.allowed_ips
    bypass                     = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids = [var.subnet_id]
  }*/

  tags = var.tags
}

resource "azurerm_storage_queue" "storage_queue" {
  name                 = "ess-fulfilment-requests"
  storage_account_name = azurerm_storage_account.storage.name
}

resource "azurerm_storage_container" "storage_container" {
  name                  = "ess-fulfilment"
  storage_account_name  = azurerm_storage_account.storage.name
}