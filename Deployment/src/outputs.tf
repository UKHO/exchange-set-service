output "web_app_name" {
value = local.web_app_name
}

output "web_app_url" {
value = "https://${module.webapp_service.default_site_hostname}"
}

output "keyvault_uri"{
  value = module.key_vault.keyvault_uri
}

output "small_exchange_set_keyvault_uri"{
  value = module.fulfilment_keyvaults.small_exchange_set_keyvault_uri
}

output "small_exchange_set_webapps"{
value = [for i, webapp in module.fulfilment_webapp.small_exchange_set_web_apps : {
        webappname = webapp
        queuename  = module.fulfilment_storage.small_exchange_set_fulfilment_queues[i]
      }
    ]
}

output "medium_exchange_set_keyvault_uri"{
  value = module.fulfilment_keyvaults.medium_exchange_set_keyvault_uri
}

output "medium_exchange_set_webapps"{
value = [for i, webapp in module.fulfilment_webapp.medium_exchange_set_web_apps : {
        webappname = webapp
        queuename  = module.fulfilment_storage.medium_exchange_set_fulfilment_queues[i]
      }
    ]
}

output "large_exchange_set_keyvault_uri"{
  value = module.fulfilment_keyvaults.large_exchange_set_keyvault_uri
}

output "large_exchange_set_webapps"{
value = [for i, webapp in module.fulfilment_webapp.large_exchange_set_web_apps : {
        webappname = webapp
        queuename  = module.fulfilment_storage.large_exchange_set_fulfilment_queues[i]
      }
    ]
}

output "storage_connection_string" {
   value = module.fulfilment_storage.small_exchange_set_connection_string
   sensitive = true
}

output "web_app_resource_group" {
   value = azurerm_resource_group.rg.name
}

output "ess_managed_user_identity_client_id"{
    value = module.user_identity.ess_service_client_id
}