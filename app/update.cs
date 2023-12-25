using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using AzureAppFunc.logic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace AzureAppFunc
{
    public static class Update
    {
        [FunctionName("update")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Starting update...");
            var bodyData = await new StreamReader(req.Body).ReadToEndAsync();

            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            log.LogInformation($"AZURE_SUBSCRIPTION_ID: {subscriptionId}");
                
            UpdateData r = GetUpdateDataFromRequest(
                req.Query["zone"].ToString(),
                req.Query["name"].ToString(),
                req.Query["group"].ToString(),
                req.Query["reqip"].ToString(), 
                bodyData);

            if(!r.IsValid(out string msg))
            {
                return new BadRequestObjectResult(msg);
            }

            log.LogInformation("Fetching DNS Management Client...");
            DnsManagementClient client = await GetDnsManagementClient(subscriptionId);
            UpdateDnsARecord updater = new UpdateDnsARecord(log, new AzureDnsManagementClient(client), r);

            try
            {
                // response according to: https://www.dnsomatic.com/docs/api
                Tuple<bool, string> result = await updater.PerformUpdate();
                if (result.Item1)
                {
                    log.LogInformation($"Successfully updated DNS record: {result.Item2}");
                    return new OkObjectResult(result.Item2);
                }
                else
                {
                    log.LogInformation($"Failed to update DNS record: {result.Item2}");
                    return new BadRequestObjectResult(result.Item2 ?? string.Empty);
                }
            }
                        
            catch (Exception ex)
            {
                log.LogError($"failed to exec: {ex.Message}");
            }

            return new BadRequestObjectResult($"prob. due to exception - failed on req ip: {r.reqip}");
        }

        private static async Task<DnsManagementClient> GetDnsManagementClient(string subscriptionId)
        {
            // this bit just gets the subscription ID - which the DNSManagementClient will need,
            // and using it this way means we don't need to provide the app with an environment variable.
            var credential = new DefaultAzureCredential();

            // ArmClient armClient = new ArmClient(credential);
            // var subscription = await armClient.GetDefaultSubscriptionAsync();
            
            var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { $"https://management.azure.com/.default" }));
            ServiceClientCredentials dnsCreds = new TokenCredentials(token.Token);
            
            // now fire up the DnsManagementClient using the token credentials.
            DnsManagementClient client = new DnsManagementClient(dnsCreds);
            client.SubscriptionId = subscriptionId;

            return new DnsManagementClient(dnsCreds);
        }

        public static UpdateData GetUpdateDataFromRequest(string zone, string name, string group, string reqip, string? body = null)
        {
            var d = new UpdateData()
            {
                name = name,
                zone = zone,
                resgroup = group,
                reqip = reqip
            };

            if (body == null)
                return d;

            try
            {
                dynamic? data = JsonConvert.DeserializeObject(body);
                if (data == null)
                    return d;

                d.zone = d.zone ?? data.zone;
                d.name = d.name ?? data.name;
                d.resgroup = d.resgroup ?? data.group;
                d.reqip = d.reqip ?? data.reqip;

                return d;
            }

            catch (Exception)
            {
                // NO-OP - just making it more robust
            }

            return d;
        }
    }
}
