output ess_service_identity_principal_id {
	value = azurerm_user_assigned_identity.ess_service_identity.principal_id
}

output ess_service_identity_tenant_id {
	value = azurerm_user_assigned_identity.ess_service_identity.tenant_id
}

output ess_service_identity_id {
	value = azurerm_user_assigned_identity.ess_service_identity.id
}

output ess_service_client_id {
	value = azurerm_user_assigned_identity.ess_service_identity.client_id
}

output ess_service_slot_identity_principal_id {
	value = azurerm_user_assigned_identity.ess_service_slot_identity.principal_id
}

output ess_service_slot_identity_tenant_id {
	value = azurerm_user_assigned_identity.ess_service_slot_identity.tenant_id
}

output ess_service_slot_identity_id {
	value = azurerm_user_assigned_identity.ess_service_slot_identity.id
}

output ess_service_slot_client_id {
	value = azurerm_user_assigned_identity.ess_service_slot_identity.client_id
}