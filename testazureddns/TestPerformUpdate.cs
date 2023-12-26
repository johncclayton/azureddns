using System;
using System.Threading.Tasks;
using AzureAppFunc.logic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace testazureddns;

public class WrapperForDnsManagement : IDisposable
{
    private readonly DnsManagementData _dnsManagementData;
    private readonly DnsManagement _dnsManager;
    private readonly bool _itWorked;

    public WrapperForDnsManagement(DnsManagementData data, DnsManagement mgr, bool itWorked)
    {
        _dnsManagementData = data;
        _dnsManager = mgr;
        _itWorked = itWorked;
    }

    public void Dispose()
    {
        if (_itWorked)
        {
            Console.WriteLine("Disposing, IP Address is " + _dnsManagementData.RequestedIpAddress);
            _dnsManager.DeleteDnsRecordSetAsync(_dnsManagementData).GetAwaiter().GetResult();
        }
    }
}


public class TestPerformUpdate
{
    
    [Theory]
    [InlineData("z", "n", "g", "bad.ip.address", false, "911")]
    [InlineData("effective-flow.com", "n", "effectiveflownotarg", "8.8.8.8", false, "azure error")]
    [InlineData("effective-flow.com", "n", "effectiveflowrg", "8.8.8.8", true, "good 8.8.8.8")]
    public async Task Test_DnsManagement(string zone, string recordSetName, 
        string resourceGroup, string requestedIpAddress, bool expected, string expectedstartofmsg)
    {
        DnsManagementData r = new DnsManagementData(zone, recordSetName, resourceGroup, 
            requestedIpAddress);
        DnsManagement mgr = new DnsManagement();

        var result = await mgr.UpdateDnsRecordSetAsync(r);
        
        // future note: This just cleans up the DNS entry if the result.item1 is true.
        using WrapperForDnsManagement wrapper = new WrapperForDnsManagement(r, mgr, result.Item1);
        
        Assert.Equal(expected, result.Item1);
        Assert.StartsWith(expectedstartofmsg, result.Item2);
    }
    
    [Fact]
    public async Task Test_NewlyCreatedRecordCanBeChanged()
    {
        DnsManagementData r = new DnsManagementData("effective-flow.com", "abc", "effectiveflowrg", "1.2.3.4");
        DnsManagement mgr = new DnsManagement();
        
        var result = await mgr.UpdateDnsRecordSetAsync(r);
        
        // future note: This just cleans up the DNS entry if the result.item1 is true.
        using WrapperForDnsManagement wrapper = new WrapperForDnsManagement(r, mgr, result.Item1);

        Assert.True(result.Item1);
        Assert.StartsWith("good 1.2.3.4", result.Item2);
        
        var result2 = await mgr.UpdateDnsRecordSetAsync(r);
        
        Assert.True(result2.Item1);
        Assert.StartsWith("nochg 1.2.3.4", result2.Item2);

        r.RequestedIpAddress = "2.3.4.5";

        var result3 = await mgr.UpdateDnsRecordSetAsync(r);
        
        Assert.True(result3.Item1);
        Assert.StartsWith("good 2.3.4.5", result3.Item2);
    }
}