output storage_account_name {
  value = azurerm_storage_account.storage.name
}

output storage_connection_string {
  value = azurerm_storage_account.storage.primary_connection_string
  sensitive = true
}

output storage_primary_access_key {
  value = azurerm_storage_account.storage.primary_access_key
  sensitive = true
}