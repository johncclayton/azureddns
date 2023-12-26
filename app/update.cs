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
            // var bodyData = await new StreamReader(req.Body).ReadToEndAsync();
            
            DnsManagementData r = new DnsManagementData(
                req.Query["zone"].ToString(),
                req.Query["name"].ToString(),
                req.Query["group"].ToString(),
                req.Query["reqip"].ToString() 
                );

            if(!r.IsValid(out string msg))
            {
                log.LogInformation("Payload not valid, aborting");
                return new BadRequestObjectResult(msg);
            }

            DnsManagement mgr = new DnsManagement();
            var result = await mgr.UpdateDnsRecordSetAsync(r);
            if (result.Item1 == false)
            {
                log.LogInformation($"Update failed, {result.Item2}, aborting");
                return new BadRequestObjectResult(result.Item2);
            } 
            
            log.LogInformation($"Update succeeded, returning: {result.Item2}");
            return new OkObjectResult(result.Item2);
        }
    }
}
