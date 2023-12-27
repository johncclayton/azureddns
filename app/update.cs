using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureAppFunc.logic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

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
            
            DnsManagementZone z = DnsManagementZone.TryParse(
                req.Query["zone"].ToString(),
                req.Query["name"].ToString(),
                req.Query["group"].ToString(),
                req.Query["reqip"].ToString() 
                );

            if (0 == z.ChildRecordSet.Count)
            {
                log.LogInformation($"No valid record sets / names found, name param is probably, aborting");
                return new BadRequestObjectResult("911 no valid record sets / names found, name param is probably, aborting");
            }
            
            foreach(var r in z.ChildRecordSet)
            {
                log.LogInformation($"Validating RecordSet: {r.RecordSetName}, IP: {r.RequestedIpAddress}");

                var valid = r.IsValid();
                if(!valid.Item1)
                {
                    log.LogInformation($"RecordSet: {r.RecordSetName}, IP: {r.RequestedIpAddress} not valid, {valid.Item2}, aborting");
                    return new BadRequestObjectResult(valid.Item2);
                }
            }

            List<Tuple<bool, string>> results = new();
            foreach(var childRs in z.ChildRecordSet)
            {
                log.LogInformation($"Updating RecordSet: {childRs.RecordSetName}, IP: {childRs.RequestedIpAddress}");
                var result = await childRs.UpdateDnsRecordSetAsync();
                results.Add(result);
                if (result.Item1 == false)
                {
                    log.LogInformation($"Update failed on {childRs.RecordSetName}, {result.Item2}, aborting");
                    return new BadRequestObjectResult(result.Item2);
                } 
            }
            
            // just pick the first as the one to return status for.
            var theFirst = results.First();
            log.LogInformation($"Update succeeded, returning: {theFirst.Item2}");
            return new OkObjectResult(theFirst.Item2);
        }
    }
}
