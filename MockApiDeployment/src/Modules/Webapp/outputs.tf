output "fulfillment_webapp" {
  value = azurerm_windows_web_app.fulfillment_webapp.name
}

output "web_app_object_id_fulfillment" {
  value = azurerm_windows_web_app.fulfillment_webapp.identity.0.principal_id
}

output "default_site_hostname_fulfillment" {
  value = azurerm_windows_web_app.fulfillment_webapp.default_site_hostname
}

output "scs_fss_mock_webapp" {
  value = azurerm_windows_web_app.scs_fss_mock_webapp.name
}

output "web_app_object_id_scs_fss_mock" {
  value = azurerm_windows_web_app.scs_fss_mock_webapp.identity.0.principal_id
}

output "default_site_hostname_scs_fss_mock" {
  value = azurerm_windows_web_app.scs_fss_mock_webapp.default_site_hostname
}

output "ess_webapp" {
  value = azurerm_windows_web_app.ess_webapp.name
}

output "web_app_object_id_ess" {
  value = azurerm_windows_web_app.ess_webapp.identity.0.principal_id
}

output "default_site_hostname_ess" {
  value = azurerm_windows_web_app.ess_webapp.default_site_hostname
}
