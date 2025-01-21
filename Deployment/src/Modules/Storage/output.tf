output ess_storage_name {
  value = azurerm_storage_account.ess_storage.name
}

output ess_storage_connection_string {
  value = azurerm_storage_account.ess_storage.primary_connection_string
  sensitive = true
}

output ess_storage_primary_access_key {
  value = azurerm_storage_account.ess_storage.primary_access_key
  sensitive = true
}
