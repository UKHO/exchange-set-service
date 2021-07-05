output small_exchange_set_name {
  value = azurerm_storage_account.small_exchange_set_storage.name
}

output small_exchange_set_connection_string {
  value = azurerm_storage_account.small_exchange_set_storage.primary_connection_string
  sensitive = true
}

output small_exchange_set_primary_access_key {
  value = azurerm_storage_account.small_exchange_set_storage.primary_access_key
  sensitive = true
}

output small_exchange_set_fulfilment_queues {
  value = azurerm_storage_queue.small_exchange_set_storage_queue[*].name
}