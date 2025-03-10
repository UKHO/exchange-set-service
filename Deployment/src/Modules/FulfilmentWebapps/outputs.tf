output "small_exchange_set_web_apps"{
  value = azurerm_app_service.small_exchange_set_webapp[*].name
}

output "medium_exchange_set_web_apps"{
  value = azurerm_app_service.medium_exchange_set_webapp[*].name
}

output "large_exchange_set_web_apps"{
  value = azurerm_app_service.large_exchange_set_webapp[*].name
}

output "small_exchange_set_scm_credentials"{
    value = merge({
    for webapp in azurerm_app_service.small_exchange_set_webapp.* :  "${webapp.name}-scm-username" =>  webapp.site_credential[0].username 
    },
    {
    for webapp in azurerm_app_service.small_exchange_set_webapp.* :  "${webapp.name}-scm-password" =>  webapp.site_credential[0].password 
    })
  sensitive = true
}

output "medium_exchange_set_scm_credentials"{
    value = merge({
    for webapp in azurerm_app_service.medium_exchange_set_webapp.* :  "${webapp.name}-scm-username" =>  webapp.site_credential[0].username 
    },
    {
    for webapp in azurerm_app_service.medium_exchange_set_webapp.* :  "${webapp.name}-scm-password" =>  webapp.site_credential[0].password 
    })
  sensitive = true
}

output "large_exchange_set_scm_credentials"{
    value = merge({
    for webapp in azurerm_app_service.large_exchange_set_webapp.* :  "${webapp.name}-scm-username" =>  webapp.site_credential[0].username 
    },
    {
    for webapp in azurerm_app_service.large_exchange_set_webapp.* :  "${webapp.name}-scm-password" =>  webapp.site_credential[0].password 
    })
  sensitive = true
}

output "asp" {
  value = {
    sxs = [for a in azurerm_service_plan.small_exchange_set_app_service_plan : { id = a.id, name = a.name }]
    mxs = [for a in azurerm_service_plan.medium_exchange_set_app_service_plan : { id = a.id, name = a.name }]
    lxs = [for a in azurerm_service_plan.large_exchange_set_app_service_plan : { id = a.id, name = a.name }]
  }
}
