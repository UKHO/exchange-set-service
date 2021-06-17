output "small_exchange_set_web_apps"{
  value = azurerm_app_service.small_exchange_set_webapp[*].name
}