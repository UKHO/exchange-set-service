output "small_exchange_set_web_apps"{
  value = azurerm_app_service.small_exchange_set_webapp[*].name
}

output "medium_exchange_set_web_apps"{
  value = azurerm_app_service.medium_exchange_set_webapp[*].name
}

output "large_exchange_set_web_apps"{
  value = azurerm_app_service.large_exchange_set_webapp[*].name
}