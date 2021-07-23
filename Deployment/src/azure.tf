terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=2.58.0"
    }
  }

  required_version = "=1.0.2"
  backend "azurerm" {
    container_name = "tfstate"
    key            = "terraform.deployment.tfplan"
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