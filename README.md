# Security

To make a call into the HTTP API, you will need an access code.  This is because anonymous calls are 
switched off for this app. 

To get the key, go to the Azure Function, and in the App Keys section either create one or pick the Default
key.  

The URL to call into the app will then use the ``code`` query parameter - and will look something like this (this isn't the real key):

    https://azdnsrg-prod.azurewebsites.net/api/update?code=PyDaTrKk7fjbP6VbgzjhxY5y3/webSaTNDE4L06xnvzvZjJVER3o/g==&zone=cluster8.tech&group=developmentrg&reqip=__MYIP__

# Synology NAS

The Synology NAS has a built in DDNS client, but it doesn't support Azure DNS.  So we need to use the
Custom option.  

To create a custom provider, go to Control Panel > External Access > DDNS.  Click Add, and then Customize Provider.

Use the 'Add' button to create a new Custom Provider.  Please follow the example below:
> https://myfuncappname123.azurewebsites.net/api/update?code=<yourcodegoeshere>&zone=<your.azure.zone.name.com>&group=<azure resource group name>&reqip=__MYIP__&name=__HOSTNAME__&extras=__USERNAME__

You'll be filling the following fields: 

| Field Name | Value                                                                                                                 |
|------------|-----------------------------------------------------------------------------------------------------------------------|
| code       | Paste in the Azure Function App key (should end in == as its base64)                                                  |
| zone       | Enter the name of the Azure DNS Zone you are managing                                                                 |
| group      | Enter the name of the Azure Resource Group that the zone is in                                                        |
| reqip      | This is the requested IP, enter in __MYIP__ - the real external IP will be placed here when the DynDns request is run |
| name       | This is the name of the DNS record you want to update, enter in __HOSTNAME__ - the real hostname will be placed here  |
| extras     | This is a comma delimited set of additional host names.                                                               |

> The 'extras' field exists because of a design decision by Synology.  They allow each "Custom Provider" to be used one time.  So if you 
> want to update multiple records, you need to create multiple Custom Providers. This is a pain, as you need to create a new Custom Provider.  
> The 'extras' field allows you to specify additional host names to update.  The Azure Function will update the IP address for each host name specified in the 'extras' field _as well as the name field_.  
> The 'extras' field is optional. 

The Synology NAS will replace the ``__MYIP__`` and ``__HOSTNAME__`` with the current IP address and hostname 
respectively.  The ``__USERNAME__`` and __PASSWORD__ values is not required, as you already deployed this to your own Azure instance
and have an associated System Assigned Identity for which you have RBAC control.

# Deployment

Before deploying the azureddns function app, you will require the following:
 * DNS Zone in Azure
 * The resource group name for the function app
 
To deploy this application, check out the code and configure the Pulumi.prod.yaml file with your own values.  

The following table lists the values you can set / configure: 

| Name                           | Description                                                                                                   | Default           |
|--------------------------------|---------------------------------------------------------------------------------------------------------------|-------------------|
| azure-native:location          | The location of the deployment in Azure                                                                       | WestUS            |
| azureddns:errorDocument        | The name of the error.html document, I have not applied / tested any logic with this one                      | error.html        |
| azureddns:indexDocument        | The name of the index.html document, I have not applied / tested any logic with this one                      | index.html        |
| azureddns:dnsZoneResourceGroup | The resource group name of the existing Azure DNS Zone within which you want to maintain A records via DynDNS | effectiveflowrg   |
| azureddns:dnsZoneName          | The name of the DNS Zone within the resource group specified by azureddns:dnsZoneResourceGroup                | effectiveflow.com |
| azureddns:resourceGroup        | The stem of the resource group name to be created for the Azure Function App                                  | azdnsrg           |

# CI/CD

If you want to run this as part of a CI/CD pipeline, you'll need a service principal with the following permissions:
 * Contributor on subscription (to create resource group + function app)

Make sure you provide the following secrets to the pipeline file, or configure them in the pipeline itself:

 * ARM_USE_MSI: true
 * PULUMI_ACCESS_TOKEN: ${{ secrets.PULUMI_ACCESS_TOKEN }}
 * AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
 * AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
 * AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
