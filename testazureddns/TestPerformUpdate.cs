using System;
using System.Threading.Tasks;
using AzureAppFunc.logic;
using NuGet.Frameworks;
using Xunit;

namespace testazureddns;

public class WrapperForDnsManagement : IDisposable
{
    private readonly DnsManagementRecordSet _dnsManagementRecordSet;
    private readonly bool _itWorked;

    public WrapperForDnsManagement(DnsManagementRecordSet recordSet, bool itWorked)
    {
        _dnsManagementRecordSet = recordSet;
        _itWorked = itWorked;
    }

    public void Dispose()
    {
        if (_itWorked)
        {
            Console.WriteLine("Disposing, IP Address is " + _dnsManagementRecordSet.RequestedIpAddress);
            _dnsManagementRecordSet.DeleteDnsRecordSetAsync().GetAwaiter().GetResult();
        }
    }
}


public class TestPerformUpdate
{
    [Theory]
    [InlineData("z", "n", "g", "bad.ip.address", false, "911")]
    [InlineData("effective-flow.com", "n", "effectiveflownotarg", "8.8.8.8", false, "azure error")]
    public async Task Test_DnsManagementWhereAzureThrowsAnException(string zone, string recordSetName, 
        string resourceGroup, string requestedIpAddress, bool expected, string expectedstartofmsg)
    {
        DnsManagementZone z = DnsManagementZone.TryParse(zone, recordSetName, resourceGroup, requestedIpAddress);
        
        Assert.Single(z.ChildRecordSet);

        await Assert.ThrowsAsync<Azure.RequestFailedException>(async () =>
        {
            var result = await z.ChildRecordSet[0].UpdateDnsRecordSetAsync();
        
            // future note: This just cleans up the DNS entry if the result.item1 is true.
            using WrapperForDnsManagement wrapper = new WrapperForDnsManagement(z.ChildRecordSet[0], result.Item1);
        
            Assert.Equal(expected, result.Item1);
            Assert.StartsWith(expectedstartofmsg, result.Item2);
        });
    }

    [Theory]
    [InlineData("effective-flow.com", "effectiveflowrg", "")]
    [InlineData("effective-flow.com", "effectiveflowrg", " ")]
    [InlineData("effective-flow.com", "effectiveflowrg", "\t")]
    public void Test_ParsingWithNoNameFails(string zone, string resourceGroup, string name)
    {
        DnsManagementZone z = DnsManagementZone.TryParse(zone, resourceGroup, name, "1.2.3.4");
        Assert.Empty(z.ChildRecordSet);
    }
    
    [Fact]
    public async Task Test_CanCreateMultipleRecords()
    {
        DnsManagementZone z = DnsManagementZone.TryParse("effective-flow.com", "effectiveflowrg", "abc, def", "1.2.3.4");
        Assert.Equal(2, z.ChildRecordSet.Count);
        
        var result1 = await z.ChildRecordSet[0].UpdateDnsRecordSetAsync();
        var result2 = await z.ChildRecordSet[1].UpdateDnsRecordSetAsync();
        
        Assert.Equal("abc", z.ChildRecordSet[0].RecordSetName);
        Assert.Equal("def", z.ChildRecordSet[1].RecordSetName);
        
        using WrapperForDnsManagement wrapper1 = new WrapperForDnsManagement(z.ChildRecordSet[0], result1.Item1);
        using WrapperForDnsManagement wrapper2 = new WrapperForDnsManagement(z.ChildRecordSet[1], result2.Item1);
        
        Assert.True(result1.Item1);
        Assert.StartsWith("good 1.2.3.4", result1.Item2);
        Assert.True(result2.Item1);
        Assert.StartsWith("good 1.2.3.4", result2.Item2);
    }
    
    [Fact]
    public async Task Test_NewlyCreatedRecordCanBeChanged()
    {
        DnsManagementZone z = DnsManagementZone.TryParse("effective-flow.com",  "effectiveflowrg", "abc", "1.2.3.4");
        Assert.Single(z.ChildRecordSet);
        
        var result = await z.ChildRecordSet[0].UpdateDnsRecordSetAsync();
        
        // future note: This just cleans up the DNS entry if the result.item1 is true.
        using WrapperForDnsManagement wrapper = new WrapperForDnsManagement(z.ChildRecordSet[0], result.Item1);

        Assert.True(result.Item1);
        Assert.StartsWith("good 1.2.3.4", result.Item2);
        
        // re-request - should result in a nochg message
        var result2 = await z.ChildRecordSet[0].UpdateDnsRecordSetAsync();
        
        Assert.True(result2.Item1);
        Assert.StartsWith("nochg 1.2.3.4", result2.Item2);

        // change the IP address that is being requested by the first record
        z.ChildRecordSet[0].RequestedIpAddress = "2.3.4.5";

        var result3 = await z.ChildRecordSet[0].UpdateDnsRecordSetAsync();
        
        Assert.True(result3.Item1);
        Assert.StartsWith("good 2.3.4.5", result3.Item2);
    }
}