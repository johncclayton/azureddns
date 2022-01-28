
provider "azurerm" {
  version = "= 2.94.0"
  features {}

  client_id         = var.spn-secret-id
  client_secret     = var.spn-secret-value
  tenant_id         = var.spn-tenant-id
}