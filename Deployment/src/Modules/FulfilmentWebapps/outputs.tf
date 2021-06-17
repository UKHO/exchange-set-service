output "small_exchange_set_web_app_object_ids" {
  value = zipmap(azurerm_app_service.small_exchange_set_webapp[*].name, azurerm_app_service.small_exchange_set_webapp[*].identity.0.principal_id)
}

output "small_exchange_set_web_apps"{
  value = azurerm_app_service.small_exchange_set_webapp[*].name
}