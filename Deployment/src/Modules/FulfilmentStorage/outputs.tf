#small exchange set
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

output queue_resource_uri_sxs {
  value = [for i in azurerm_storage_queue.small_exchange_set_storage_queue : "${azurerm_storage_account.small_exchange_set_storage.id}/services/queue/queues/${i.name}"]
}

#medium exchange set
output medium_exchange_set_name {
  value = azurerm_storage_account.medium_exchange_set_storage.name
}

output medium_exchange_set_connection_string {
  value = azurerm_storage_account.medium_exchange_set_storage.primary_connection_string
  sensitive = true
}

output medium_exchange_set_primary_access_key {
  value = azurerm_storage_account.medium_exchange_set_storage.primary_access_key
  sensitive = true
}

output medium_exchange_set_fulfilment_queues {
  value = azurerm_storage_queue.medium_exchange_set_storage_queue[*].name
}

output queue_resource_uri_mxs {
  value = [for i in azurerm_storage_queue.medium_exchange_set_storage_queue : "${azurerm_storage_account.medium_exchange_set_storage.id}/services/queue/queues/${i.name}"]
}

#large exchange set
output large_exchange_set_name {
  value = azurerm_storage_account.large_exchange_set_storage.name
}

output large_exchange_set_connection_string {
  value = azurerm_storage_account.large_exchange_set_storage.primary_connection_string
  sensitive = true
}

output large_exchange_set_primary_access_key {
  value = azurerm_storage_account.large_exchange_set_storage.primary_access_key
  sensitive = true
}

output large_exchange_set_fulfilment_queues {
  value = azurerm_storage_queue.large_exchange_set_storage_queue[*].name
}

output queue_resource_uri_lxs {
  value = [for i in azurerm_storage_queue.large_exchange_set_storage_queue : "${azurerm_storage_account.large_exchange_set_storage.id}/services/queue/queues/${i.name}"]
}
