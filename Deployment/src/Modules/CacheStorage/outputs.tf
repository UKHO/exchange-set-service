output cache_storage_name {
  value = azurerm_storage_account.ess_cache_storage.name
}

output cache_storage_connection_string {
  value = azurerm_storage_account.ess_cache_storage.primary_connection_string
  sensitive = true
}

output cache_storage_primary_access_key {
  value = azurerm_storage_account.ess_cache_storage.primary_access_key
  sensitive = true
}

output cache_storage1_name {
  value = azurerm_storage_account.ess_cache_storage1.name
}

output cache_storage1_connection_string {
  value = azurerm_storage_account.ess_cache_storage1.primary_connection_string
  sensitive = true
}

output cache_storage1_primary_access_key {
  value = azurerm_storage_account.ess_cache_storage1.primary_access_key
  sensitive = true
}

output cache_storage2_name {
  value = azurerm_storage_account.ess_cache_storage2.name
}

output cache_storage2_connection_string {
  value = azurerm_storage_account.ess_cache_storage2.primary_connection_string
  sensitive = true
}

output cache_storage2_primary_access_key {
  value = azurerm_storage_account.ess_cache_storage2.primary_access_key
  sensitive = true
}