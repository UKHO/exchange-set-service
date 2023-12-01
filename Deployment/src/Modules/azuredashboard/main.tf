data "azurerm_subscription" "current" {
}


resource "azurerm_dashboard" "azure-dashboard" {
  name                = var.name
  resource_group_name = var.resource_group.name
  location            = var.resource_group.location
  tags                = var.tags    
  dashboard_properties = templatefile("${path.module}/dashboard.tpl",
    {
      subscription_id = data.azurerm_subscription.current.subscription_id, 
      environment = var.environment
  })
}