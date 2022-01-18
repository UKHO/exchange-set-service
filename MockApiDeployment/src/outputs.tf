output "ess_webappname" {
value = module.webapp_service.ess_webapp
}

output "mock_webappname" {
value = module.webapp_service.scs_fss_mock_webapp
}

output "ess_fulfilment_webappname" {
value = module.webapp_service.fulfillment_webapp
}

output "ess_fulfillment_web_app_url" {
value = "https://${module.webapp_service.default_site_hostname_fulfillment}"
}

output "scs_fss_mock_web_app_url" {
value = "https://${module.webapp_service.default_site_hostname_scs_fss_mock}"
}

output "ess_web_app_url" {
value = "https://${module.webapp_service.default_site_hostname_ess}"
}

output "webapp_rg" {
value = azurerm_resource_group.webapp_rg.name
}

output "keyvault_uri"{
  value = module.key_vault.keyvault_uri
}

output "ess_managed_user_identity_client_id"{
    value = module.user_identity.service_client_id
}

output "storage_account_queue_name"{
    value = module.storage.storage_account_queue_name
}

output "qc_storage_connection_string"{
    value = module.storage.storage_connection_string
    sensitive = true
}