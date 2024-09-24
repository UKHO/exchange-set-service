terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.116.0"
    }
  }

  required_version = "=1.9.6"
  backend "azurerm" {
    container_name = "tfstate"
  }
}

provider "azurerm" {
  features {}
}
