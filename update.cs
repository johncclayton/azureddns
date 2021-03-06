using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Management.Dns;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Rest;
using Azure.Core;

namespace azureddns
{
    public static class update
    {
        [FunctionName("update")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var bodyData = await new StreamReader(req.Body).ReadToEndAsync(); 
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

            DnsManagementClient client = await GetDNSManagementClient();
            UpdateDNS_ARecord updater = new UpdateDNS_ARecord(log, new AzureDnsManagementClient(client), r);

            try
            {
                // response according to: https://www.dnsomatic.com/docs/api
                Tuple<bool, string> result = await updater.PerformUpdate();
                if(result.Item1)
                    return new OkObjectResult(result.Item2);
                else
                    return new BadRequestObjectResult(result.Item2 ?? string.Empty);
            }
                        
            catch (Exception ex)
            {
                log.LogError($"failed to exec: {ex.Message}");
            }

            return new BadRequestObjectResult($"fail {r.reqip}");
        }

        private static async Task<DnsManagementClient> GetDNSManagementClient()
        {
            // this bit just gets the subscription ID - which the DNSManagementClient will need,
            // and using it this way means we don't need to provide the app with an environment variable.
            var defaultClient = new DefaultAzureCredential();

            ArmClient armClient = new ArmClient(defaultClient);
            Subscription subscription = await armClient.GetDefaultSubscriptionAsync();

            // this fetches a token for the management layer, making use of the managed system identity that Terraform set up. 
            var token = await defaultClient.GetTokenAsync(new TokenRequestContext(new[] { $"https://management.azure.com/.default" }));
            ServiceClientCredentials dnsCreds = new TokenCredentials(token.Token);

            // now fire up the DnsManagementClient using the token crendentials.
            DnsManagementClient client = new DnsManagementClient(dnsCreds);
            client.SubscriptionId = subscription.Data.SubscriptionGuid;

            return client;
        }

        public static UpdateData GetUpdateDataFromRequest(string zone, string name, string group, string reqip, string body = null)
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
                dynamic data = JsonConvert.DeserializeObject(body);

                d.zone = d.zone ?? data?.zone;
                d.name = d.name ?? data?.name;
                d.resgroup = d.resgroup ?? data?.group;
                d.reqip = d.reqip ?? data?.reqip;

                return d;
            }

            catch (Exception e)
            {
                // NO-OP - just making it more robust
            }

            return d;
        }
    }
}
