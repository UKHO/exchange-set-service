terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.39.1"
    }
  }
  required_version = "=1.3.7"
  backend "azurerm" {
    container_name = "tfstate"
    key            = "mockapiterraform.deployment.tfplan"
  }
}

provider "azurerm" {
  features { }
}
