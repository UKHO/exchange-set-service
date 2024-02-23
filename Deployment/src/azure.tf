terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.89.0"
    }
  }

  required_version = "=1.7.2"
  backend "azurerm" {
    container_name = "tfstate"
    key            = "terraform.deployment.v2.tfplan"
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