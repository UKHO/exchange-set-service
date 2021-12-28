output cache_storage_name {
  value = azurerm_storage_account.cache_storage_ess.name
}

output cache_storage_connection_string {
  value = azurerm_storage_account.cache_storage_ess.primary_connection_string
  sensitive = true
}

output cache_storage_primary_access_key {
  value = azurerm_storage_account.cache_storage_ess.primary_access_key
  sensitive = true
}