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
    #key            = "terraform.ess.apim.deployment.tfplan"
  }
}

provider "azurerm" {
  features {}
}
