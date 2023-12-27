using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.Network.V20230701Preview;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using Pulumi.Command.Local;
using static Pulumi.AzureNative.Web.ManagedServiceIdentityType;
using Deployment = Pulumi.Deployment;
using SyncedFolder = Pulumi.SyncedFolder;

return await Deployment.RunAsync(() =>
{
    // Import the program's configuration settings.
    var config = new Config();

    var sitePath = config.Get("sitePath") ?? "../www";
    var appPath = config.Get("appPath") ?? "../app";

    var indexDocument = config.Get("indexDocument") ?? "index.html";
    var errorDocument = config.Get("errorDocument") ?? "error.html";

    // Create a resource group for the website.
    var resourceGroup = new ResourceGroup(config.Require("resourceGroup"), new ResourceGroupArgs()
    {
        ResourceGroupName = config.Require("resourceGroup")
    });

    // Create a blob storage account.
    var account = new StorageAccount("account", new StorageAccountArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Kind = Kind.StorageV2,
        Sku = new SkuArgs
        {
            Name = SkuName.Standard_LRS,
        },
    });

    // Create a storage container for the pages of the website.
    var website = new StorageAccountStaticWebsite("website", new StorageAccountStaticWebsiteArgs
    {
        AccountName = account.Name,
        ResourceGroupName = resourceGroup.Name,
        IndexDocument = indexDocument,
        Error404Document = errorDocument,
    });

    // Use a synced folder to manage the files of the website.
    var syncedFolder = new SyncedFolder.AzureBlobFolder("synced-folder", new SyncedFolder.AzureBlobFolderArgs
    {
        Path = sitePath,
        ResourceGroupName = resourceGroup.Name,
        StorageAccountName = account.Name,
        ContainerName = website.ContainerName,
    });

    // Create a storage container for the serverless app.
    var appContainer = new BlobContainer("app-container", new BlobContainerArgs
    {
        AccountName = account.Name,
        ResourceGroupName = resourceGroup.Name,
        PublicAccess = PublicAccess.None,
    });

    // Compile the the app.
    var outputPath = "publish";
    var publishCommand = Run.Invoke(new RunInvokeArgs
    {
        Command = $"dotnet publish --output {outputPath}",
        Dir = appPath,
    });

    // Upload the serverless app to the storage container.
    var appBlob = new Blob("app-blob", new()
    {
        AccountName = account.Name,
        ResourceGroupName = resourceGroup.Name,
        ContainerName = appContainer.Name,
        Source = publishCommand.Apply(result => new FileArchive(Path.Combine(appPath, outputPath)) as AssetOrArchive),
    });

    // Create a shared access signature to give the Function App access to the code.
    var signature = ListStorageAccountServiceSAS.Invoke(new()
    {
        ResourceGroupName = resourceGroup.Name,
        AccountName = account.Name,
        Protocols = HttpProtocol.Https,
        SharedAccessStartTime = "2022-01-01",
        SharedAccessExpiryTime = "2025-01-01",
        Resource = SignedResource.C,
        Permissions = Permissions.R,
        ContentType = "application/json",
        CacheControl = "max-age=5",
        ContentDisposition = "inline",
        ContentEncoding = "deflate",
        CanonicalizedResource = Output.Tuple(account.Name, appContainer.Name)
            .Apply(values => $"/blob/{values.Item1}/{values.Item2}"),
    }).Apply(result => result.ServiceSasToken);

    // Create an App Service plan for the Function App.
    var plan = new AppServicePlan("plan", new()
    {
        ResourceGroupName = resourceGroup.Name,
        Sku = new SkuDescriptionArgs
        {
            Name = "Y1",
            Tier = "Dynamic",
        },
    });

    var storageAccountKeys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs
    {
        ResourceGroupName = resourceGroup.Name,
        AccountName = account.Name
    });

    var primaryStorageKey = storageAccountKeys.Apply(accountKeys =>
    {
        var firstKey = accountKeys.Keys[0].Value;
        return Output.CreateSecret(firstKey);
    });

    var accountConnectionString =
        Output.Format(
            $"DefaultEndpointsProtocol=https;AccountName={account.Name};AccountKey={primaryStorageKey};EndpointSuffix=core.windows.net");

    var app = new WebApp("app", new()
    {
        ResourceGroupName = resourceGroup.Name,
        ServerFarmId = plan.Id,
        HttpsOnly = true,
        Kind = "FunctionApp",
        Identity = new ManagedServiceIdentityArgs
        {
            Type = SystemAssigned,
        },
        SiteConfig = new SiteConfigArgs
        {
            NetFrameworkVersion = "v6.0",
            DetailedErrorLoggingEnabled = true,
            HttpLoggingEnabled = true,
            AppSettings = new[]
            {
                // new NameValuePairArgs
                // {
                //     Name = "APPINSIGHTS_INSTRUMENTATIONKEY",
                //     Value = "some-valid-instrumentation-key-goes-here"
                // },
                new NameValuePairArgs
                {
                    Name = "AzureWebJobsStorage",
                    Value = accountConnectionString,
                },
                new NameValuePairArgs
                {
                    Name = "FUNCTIONS_WORKER_RUNTIME",
                    Value = "dotnet",
                },
                new NameValuePairArgs
                {
                    Name = "FUNCTIONS_EXTENSION_VERSION",
                    Value = "~4",
                },
                new NameValuePairArgs
                {
                    Name = "WEBSITE_RUN_FROM_PACKAGE",
                    Value = Output.Tuple(account.Name, appContainer.Name, appBlob.Name, signature).Apply(values =>
                    {
                        var accountName = values.Item1;
                        var containerName = values.Item2;
                        var blobName = values.Item3;
                        var token = values.Item4;
                        return $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}?{token}";
                    }),
                },
            },
            Cors = new CorsSettingsArgs
            {
                AllowedOrigins = new[]
                {
                    "*",
                },
            },
        },
    });

    // Create a JSON configuration file for the website.
    var siteConfig = new Blob("config.json", new()
    {
        AccountName = account.Name,
        ResourceGroupName = resourceGroup.Name,
        ContainerName = website.ContainerName,
        ContentType = "application/json",
        Source = app.DefaultHostName.Apply(hostname =>
        {
            var config = JsonSerializer.Serialize(new { api = $"https://{hostname}/api" });
            return new StringAsset(config) as AssetOrArchive;
        }),
    });

    var dnsZoneRg = GetResourceGroup.Invoke(new()
    {
        ResourceGroupName = config.Require("dnsZoneResourceGroup"),
    });

    var dnsZone = GetZone.Invoke(new GetZoneInvokeArgs()
    {
        ZoneName = config.Require("dnsZoneName"),
        ResourceGroupName = dnsZoneRg.Apply(rg => rg.Name)
    });

    var currentConfig = GetClientConfig.Invoke();
    var subscriptionId = currentConfig.Apply(cc => cc.SubscriptionId);

    // DNS Zone Contributor: on the DNS Zone we're affecting.
    _ = new RoleAssignment("role-assignment", new RoleAssignmentArgs
    {
        PrincipalId = app.Identity.Apply(identity => identity!.PrincipalId),
        PrincipalType = PrincipalType.ServicePrincipal,
        RoleDefinitionId =
            Output.Format(
                $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{RoleId.DnsZoneContributorId}"),
        Scope = dnsZone.Apply(z => z.Id)
    });

    // Reader: on the resource group of the DNS Zone we're affecting.
    _ = new RoleAssignment("role-assignment-rg-read", new RoleAssignmentArgs
    {
        PrincipalId = app.Identity.Apply(identity => identity!.PrincipalId),
        PrincipalType = PrincipalType.ServicePrincipal,
        RoleDefinitionId =
            Output.Format(
                $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{RoleId.ReaderId}"),
        Scope = RoleId.ResourceGroupScope(subscriptionId, Output.Create(config.Require("dnsZoneResourceGroup")))
    });
    
    // Export the URLs of the website and serverless endpoint.
    return new Dictionary<string, object?>
    {
        ["accountConnectionString"] = accountConnectionString,
        ["siteURL"] = account.PrimaryEndpoints.Apply(primaryEndpoints => primaryEndpoints.Web),
        ["apiURL"] = app.DefaultHostName.Apply(defaultHostName => $"https://{defaultHostName}/api"),
    };
});

static class RoleId
{
    public static Output<string> ResourceGroupScope(Output<string> subsId, Output<string> groupName) =>
        Output.Format($"/subscriptions/{subsId}/resourcegroups/{groupName}");

    public static readonly string ReaderId = "acdd72a7-3385-48ef-bd42-f606fba81ae7";
    public static readonly string DnsZoneContributorId = "befefa01-2a29-4197-83a8-272ff33ce314";
}