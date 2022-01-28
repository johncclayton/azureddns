terraform {
  backend "azurerm" {
    storage_account_name = "state9574a46c"
    container_name       = "azureddns"
    key                  = "tf-azureddns.tfstate"
  }

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>2.0"
    }
  }
}
