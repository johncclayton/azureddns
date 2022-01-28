
resource "azurerm_resource_group" "rg" {
  name     = title("${var.prefix}")
  location = "${var.location}"
}

resource "azurerm_storage_account" "storage" {
    name = "${random_string.storage_name.result}"
    resource_group_name = "${azurerm_resource_group.rg.name}"
    location = "${var.location}"
    account_tier = "Standard"
    account_replication_type = "LRS"
}

resource "azurerm_storage_container" "deployments" {
    name = "function-releases"
    storage_account_name = "${azurerm_storage_account.storage.name}"
    container_access_type = "private"
}

resource "azurerm_storage_blob" "appcode" {
    name = "functionapp.zip"
    storage_account_name = "${azurerm_storage_account.storage.name}"
    storage_container_name = "${azurerm_storage_container.deployments.name}"
    type = "Block"
    source = "${var.functionapp}"
}

data "azurerm_storage_account_sas" "sas" {
    connection_string = "${azurerm_storage_account.storage.primary_connection_string}"
    https_only = true
    start = "2021-11-30"
    expiry = "2022-12-31"
    resource_types {
        object = true
        container = false
        service = false
    }
    services {
        blob = true
        queue = false
        table = false
        file = false
    }
    permissions {
        read = true
        write = false
        delete = false
        list = false
        add = false
        create = false
        update = false
        process = false
    }
}

resource "azurerm_application_insights" "insights" {
  name                = "${var.prefix}-insights"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
}

resource "azurerm_app_service_plan" "asp" {
  name                = "${var.prefix}-plan"
  location            = "${var.location}"
  resource_group_name = azurerm_resource_group.rg.name

  kind = "FunctionApp"

  sku {
    tier = "Dynamic"
    size = "Y1"
  }
}

data "azurerm_subscription" "primary" {
}

resource "azurerm_function_app" "functions" {
    name = "${var.prefix}-${var.environment}"
    location = "${var.location}"
    resource_group_name = "${azurerm_resource_group.rg.name}"
    app_service_plan_id = "${azurerm_app_service_plan.asp.id}"
    storage_connection_string = "${azurerm_storage_account.storage.primary_connection_string}"

    version = "~4"

    identity {
      type = "SystemAssigned"
    }

    app_settings = {
        https_only = true
        FUNCTIONS_WORKER_RUNTIME = "dotnet"
        FUNCTIONS_EXTENSION_VERSION = "~4"
        FUNCTION_APP_EDIT_MODE = "readonly"
        APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.insights.instrumentation_key}"
        AzureWebJobsStorage = "${azurerm_storage_account.storage.primary_connection_string}"
        HASH = "${base64encode(filesha256("${var.functionapp}"))}"
        WEBSITE_RUN_FROM_PACKAGE = "https://${azurerm_storage_account.storage.name}.blob.core.windows.net/${azurerm_storage_container.deployments.name}/${azurerm_storage_blob.appcode.name}${data.azurerm_storage_account_sas.sas.sas}"
    }
}

resource "azurerm_role_assignment" "roledns" {
  scope                = data.azurerm_subscription.primary.id
  role_definition_name = "DNS Zone Contributor"
  principal_id         = azurerm_function_app.functions.identity.0.principal_id
}


