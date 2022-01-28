
provider "azurerm" {
  features {}

  client_id         = var.spn-client-id
  client_secret     = var.spn-secret-value
  tenant_id         = var.spn-tenant-id
}