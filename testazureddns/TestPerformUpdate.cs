using System;
using System.Threading.Tasks;
using AzureAppFunc.logic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace testazureddns;

public class TestPerformUpdate
{
    
    [Theory]
    [InlineData("z", "n", "g", "bad.ip.address", false, "911")]
    [InlineData("effective-flow.com", "n", "effectiveflownotarg", "8.8.8.8", false, "azure error")]
    [InlineData("effective-flow.com", "n", "effectiveflowrg", "8.8.8.8", true, "nochg 8.8.8.8")]
    public async Task Test_DnsManagement(string zone, string recordSetName, 
        string resourceGroup, string requestedIpAddress, bool expected, string expectedstartofmsg)
    {
        DnsManagementData r = new DnsManagementData(zone, recordSetName, resourceGroup, 
            requestedIpAddress);
        DnsManagement mgr = new DnsManagement();
        
        var result = await mgr.UpdateDnsRecordSetAsync(r);
        
        Assert.Equal(expected, result.Item1);
        Assert.StartsWith(expectedstartofmsg, result.Item2);

        if (result.Item1)
        {
            // always be a tidy kiwi... clean up
            await mgr.DeleteDnsRecordSetAsync(r);
        }
    }

    [Fact]
    public async Task Test_NewlyCreatedRecordCanBeChanged()
    {
        DnsManagementData r = new DnsManagementData(zone, recordSetName, resourceGroup, 
            requestedIpAddress);
        DnsManagement mgr = new DnsManagement();
        
    }
}