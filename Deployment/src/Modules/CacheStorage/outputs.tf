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

output cache_storage_name2 {
  value = azurerm_storage_account.ess_cache_storage2.name
}

output cache_storage_connection_string2 {
  value = azurerm_storage_account.ess_cache_storage2.primary_connection_string
  sensitive = true
}

output cache_storage_primary_access_key2 {
  value = azurerm_storage_account.ess_cache_storage2.primary_access_key
  sensitive = true
}

output cache_storage_name3 {
  value = azurerm_storage_account.ess_cache_storage3.name
}

output cache_storage_connection_string3 {
  value = azurerm_storage_account.ess_cache_storage3.primary_connection_string
  sensitive = true
}

output cache_storage_primary_access_key3 {
  value = azurerm_storage_account.ess_cache_storage3.primary_access_key
  sensitive = true
}
