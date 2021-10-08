output service_identity_principal_id {
	value = azurerm_user_assigned_identity.service_identity.principal_id
}

output service_identity_tenant_id {
	value = azurerm_user_assigned_identity.service_identity.tenant_id
}

output service_identity_id {
	value = azurerm_user_assigned_identity.service_identity.id
}

output service_client_id {
	value = azurerm_user_assigned_identity.service_identity.client_id
}