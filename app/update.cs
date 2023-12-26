using System;
using System.IO;
using System.Threading.Tasks;
using AzureAppFunc.logic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
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
                
            DnsManagementData r = new DnsManagementData(
                req.Query["zone"].ToString(),
                req.Query["name"].ToString(),
                req.Query["group"].ToString(),
                req.Query["reqip"].ToString() 
                );

            if(!r.IsValid(out string msg))
            {
                return new BadRequestObjectResult(msg);
            }

            DnsManagement mgr = new DnsManagement();
            var result = await mgr.UpdateDnsRecordSetAsync(r);
            if (result.Item1 == false)
            {
                return new BadRequestObjectResult(result.Item2);
            } 
            
            return new OkObjectResult(result.Item2);
        }
    }
}
