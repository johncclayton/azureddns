using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;

using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

using Microsoft.Rest;
using Azure.Core;
using System.Collections.Generic;

namespace azureddns
{
    public static class update
    {
        [FunctionName("update")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string zone = req.Query["zone"];
            string name = req.Query["name"];
            string group = req.Query["group"];
            string reqip = req.Query["reqip"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            zone = zone ?? data?.zone;
            name = name ?? data?.name;
            group = group ?? data?.group;
            reqip = reqip ?? data?.reqip;

            // this bit just gets the subscription ID - which the DNSManagementClient will need
            var defaultClient = new DefaultAzureCredential();
            ArmClient armClient = new ArmClient(defaultClient);
            Subscription subscription = await armClient.GetDefaultSubscriptionAsync();

            // this fetches a token for the management layer, making use of the managed system identity that Terraform set up. 
            var token = await defaultClient.GetTokenAsync(new TokenRequestContext(new[] { $"https://management.azure.com/.default" }));
            ServiceClientCredentials dnsCreds = new TokenCredentials(token.Token);

            // now fire up the DnsManagementClient using the token crendentials.
            DnsManagementClient client = new DnsManagementClient(dnsCreds);
            if (client.SubscriptionId == null)
            {
                client.SubscriptionId = subscription.Data.SubscriptionGuid;
            }

            if (string.IsNullOrEmpty(client.SubscriptionId))
            {
                return new BadRequestObjectResult("subscription ID could not be found in the ArmClient instance - meaning auth/dns code cannot continue");
            }

            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult("no 'name' value, use this to specify the hostname");
            }

            if(string.IsNullOrEmpty(group))
            {
                return new BadRequestObjectResult("no 'group', please specify a resource group for the zone");
            }

            if(string.IsNullOrEmpty(zone))
            {
                return new BadRequestObjectResult("no 'zone', specify the DNS zone");
            }

            if(string.IsNullOrEmpty(reqip))
            {
                return new BadRequestObjectResult("despite assumptions - you still need to supply the IP address to set");
            }

            RecordSet recordSet = null;

            try
            {
                log.LogInformation($"looking for A record in group: {group}, zone: {zone}, name: {name}");

                // get the DNS zone in this RG with that zone.
                recordSet = await client.RecordSets.GetAsync(group, zone, name, RecordType.A);
            }

            catch {}

            try
            {
                if (recordSet != null)
                {
                    log.LogInformation($"found one, I'll overwrite the the list of IPs with this: {reqip}");

                    recordSet.ARecords.Clear();
                    recordSet.ARecords.Add(new ARecord(reqip));
                    recordSet = await client.RecordSets.UpdateAsync(group, zone, name, RecordType.A, recordSet);

                    return new OkObjectResult($"good {reqip}");
                }
                else
                {
                    log.LogInformation($"creating an ARecord for name: {name}, IP: {reqip}");

                    recordSet = new RecordSet();

                    recordSet.TTL = 3600;
                    recordSet.ARecords = new List<ARecord>();
                    recordSet.ARecords.Add(new ARecord(reqip));
                    recordSet = await client.RecordSets.CreateOrUpdateAsync(group, zone, name, RecordType.A, recordSet);
                }

                if (recordSet.ARecords.Any(ip => ip.Ipv4Address == reqip))
                {
                    return new OkObjectResult($"good {reqip}");
                }
            }

            catch(Exception ex)
            {
                log.LogError($"failed to exec: {ex.Message}");
            }

            return new BadRequestObjectResult($"fail {reqip}");
        }
    }
}
