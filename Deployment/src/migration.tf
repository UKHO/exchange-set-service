# Used to migrate deprecated resources without having to destroy and recreate them.
# This file can be removed once the migration is complete in all environments, but will do no harm if left.
# See PBI #205137.

# service plans
removed {
  from = module.webapp_service.azurerm_app_service_plan.app_service_plan

  lifecycle {
    destroy = false
  }
}

import {
  to = module.webapp_service.azurerm_service_plan.app_service_plan
  id = "${azurerm_resource_group.rg.id}/providers/Microsoft.Web/serverFarms/${local.web_app_name}-asp"
}

# For web apps below see PBI #159288.
# Main webapp
removed {
  from = module.webapp_service.azurerm_app_service.webapp_service

  lifecycle {
    destroy = false
  }
}

import {
  to = module.webapp_service.azurerm_windows_web_app.webapp_service
  id = "${azurerm_resource_group.rg.id}/providers/Microsoft.Web/sites/${local.web_app_name}"
}

removed {
  from = module.webapp_service.azurerm_app_service_slot.staging

  lifecycle {
    destroy = false
  }
}

import {
  to = module.webapp_service.azurerm_windows_web_app_slot.staging
  id = "${azurerm_resource_group.rg.id}/providers/Microsoft.Web/sites/${local.web_app_name}/slots/staging"
}

# swift
removed {
  from = module.webapp_service.azurerm_app_service_virtual_network_swift_connection.webapp_vnet_integration

  lifecycle {
    destroy = false
  }
}

removed {
  from = module.webapp_service.azurerm_app_service_slot_virtual_network_swift_connection.slot_vnet_integration

  lifecycle {
    destroy = false
  }
}
