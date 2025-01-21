output ess_storage_name {
  value = azurerm_storage_account.ess_storage[0].name
}

output ess_storage_connection_string {
  value = azurerm_storage_account.ess_storage[0].primary_connection_string
  sensitive = true
}

output ess_storage_primary_access_key {
  value = azurerm_storage_account.ess_storage[0].primary_access_key
  sensitive = true
}
