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
