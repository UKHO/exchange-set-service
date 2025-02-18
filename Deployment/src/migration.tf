# Used to migrate deprecated resources without having to destroy and recreate them.
# This file can be removed once the migration is complete in all environments, but will do no harm if left.
# See PBI #205137.

# service plan
removed {
  from = module.fulfilment_webapp.azurerm_app_service_plan.small_exchange_set_app_service_plan

  lifecycle {
    destroy = false
  }
}

import {
  count = local.config_data.ESSFulfilmentConfiguration.SmallExchangeSetInstance
  to = module.fulfilment_webapp.azurerm_service_plan.small_exchange_set_app_service_plan[count.index]
  id = "${azurerm_resource_group.rg.id}/providers/Microsoft.Web/serverFarms/${local.service_name}-${local.env_name}-sxs-${sum([1, count.index])}-asp${var.suffix}"
}
