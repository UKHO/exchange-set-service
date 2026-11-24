# The same Terraform runs twice per environment: suffix "" deploys the API web app, suffix "-v2" deploys the v2 web app.
locals {
  front_door_instance               = var.suffix == "-v2" ? "v2" : "api"
  front_door_name_prefix            = "ess-${local.env_name}-${local.front_door_instance}"
  front_door_custom_domain_hostname = local.front_door_instance == "v2" ? var.front_door_v2_custom_domain_hostname : var.front_door_api_custom_domain_hostname
  front_door_custom_domain_enabled  = local.front_door_custom_domain_hostname != ""

  # Same IP allowlist as the web app; VNet subnet rules have no Front Door equivalent.
  front_door_allowed_ips = [for ip in var.allowed_ips : length(split("/", ip)) > 1 ? ip : "${ip}/32"]
}

module "front_door_delivery" {
  count  = var.front_door_enabled ? 1 : 0
  source = "github.com/UKHO/tfmodule-alz-front-door-compliant//modules/front-door-delivery?ref=v922453"

  providers = {
    azurerm = azurerm.frontdoor
  }

  front_door_profile_name   = var.front_door_profile_name
  front_door_resource_group = var.front_door_resource_group
  shared_endpoint_name      = var.front_door_shared_endpoint_name

  origin_groups = {
    ess = {
      name                     = "${local.front_door_name_prefix}-origin-group"
      session_affinity_enabled = false
      load_balancing = {
        sample_size                        = 4
        successful_samples_required        = 3
        additional_latency_in_milliseconds = 50
      }
      health_probe = {
        protocol            = "Https"
        interval_in_seconds = 30
        request_type        = "GET"
        path                = "/health"
      }
    }
  }

  # Private Link creates a pending private endpoint connection on the web app that must be approved after apply.
  origins = {
    ess_webapp = {
      name                           = "${local.front_door_name_prefix}-origin"
      origin_group_key               = "ess"
      enabled                        = true
      host_name                      = module.webapp_service.default_site_hostname
      http_port                      = 80
      https_port                     = 443
      origin_host_header             = module.webapp_service.default_site_hostname
      priority                       = 1
      weight                         = 1000
      certificate_name_check_enabled = true
      private_link = {
        request_message        = "ESS ${local.env_name} ${local.front_door_instance} Front Door private link request"
        target_type            = "sites"
        location               = module.webapp_service.webapp_service.location
        private_link_target_id = module.webapp_service.webapp_service.id
      }
    }
  }

  custom_domains = local.front_door_custom_domain_enabled ? {
    ess_domain = {
      name        = "${local.front_door_name_prefix}-domain"
      dns_zone_id = var.front_door_custom_domain_dns_zone_id != "" ? var.front_door_custom_domain_dns_zone_id : null
      host_name   = local.front_door_custom_domain_hostname
      tls         = {} # Defaults: ManagedCertificate with TLS12
    }
  } : {}

  rule_sets = var.front_door_ip_restriction_enabled ? {
    ip_restriction = {
      name = "ess${local.env_name}${local.front_door_instance}iprestriction"
    }
  } : {}

  rules = var.front_door_ip_restriction_enabled ? {
    block_non_allowed_ips = {
      name              = "BlockNonAllowedIPs"
      rule_set_key      = "ip_restriction"
      order             = 1
      behavior_on_match = "Stop"
      actions = {
        url_redirect_action = {
          redirect_type        = "TemporaryRedirect"
          redirect_protocol    = "Https"
          destination_hostname = ""
        }
      }
      conditions = {
        remote_address_condition = {
          operator         = "IPMatch"
          negate_condition = true
          match_values     = local.front_door_allowed_ips
        }
      }
    }
  } : {}

  routes = {
    ess_route = {
      name                   = "${local.front_door_name_prefix}-route"
      origin_group_key       = "ess"
      origin_keys            = ["ess_webapp"]
      enabled                = var.front_door_route_enabled
      forwarding_protocol    = "HttpsOnly"
      https_redirect_enabled = true
      patterns_to_match      = ["/*"]
      supported_protocols    = ["Http", "Https"]
      custom_domain_keys     = local.front_door_custom_domain_enabled ? ["ess_domain"] : []
      link_to_default_domain = !local.front_door_custom_domain_enabled
      rule_set_keys          = var.front_door_ip_restriction_enabled ? ["ip_restriction"] : []
    }
  }
}
