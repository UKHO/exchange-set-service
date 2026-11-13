terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=4.81.0"
    }
  }

  required_version = "=1.15.8"
  backend "azurerm" {
    container_name = "tfstate"
  }
}

provider "azurerm" {
  features {}
}

provider "azurerm" {
  features {}
  alias = "build_agent"
  subscription_id = var.agent_subscription_id
}

provider "azurerm" {
  features {}
  alias           = "frontdoor"
  subscription_id = var.front_door_subscription_id != "" ? var.front_door_subscription_id : null
}
