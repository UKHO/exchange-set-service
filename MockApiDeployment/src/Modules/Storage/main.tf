resource "azurerm_storage_account" "storage" {
  name = var.name
  resource_group_name = var.resource_group_name
  location = var.location
  account_tier = "Standard"
  account_replication_type = "LRS"
  account_kind = "StorageV2"
  tags = var.tags
}

resource "azurerm_storage_container" "exchange_set_storage_container" {
  name                  = "ess-fulfilment"
  storage_account_name  = azurerm_storage_account.storage.name
}

resource "azurerm_storage_queue" "exchange_set_storage_queue" {
  name                 = "ess-1-fulfilment"
  storage_account_name = azurerm_storage_account.storage.name
}

resource "azurerm_storage_container" "ess_storage_container" {
  name                 = "ess-fulfilment-container"
  storage_account_name = azurerm_storage_account.storage.name
}

resource "azurerm_storage_queue" "ess_storage_queue" {
  name                 = "ess-fulfilment-queue"
  storage_account_name = azurerm_storage_account.storage.name
}
