using System;
using System.Threading.Tasks;
using AzureAppFunc;
using AzureAppFunc.logic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace testazureddns;

public class TestPerformUpdate
{
    private readonly ILogger _fakeLogger = new NullLogger<TestPerformUpdate>();
    private readonly UpdateData _updateData = Update.GetUpdateDataFromRequest("z", "n", "g", "ip");
    
    [Fact]
    public async Task TestPerformUpdate_WhenDNSIsNotValid()
    {
        var fakeDNS = new FakeDnsManagementClient() { ExistingIp = null, Valid = false };
        UpdateDnsARecord t = new UpdateDnsARecord(_fakeLogger, fakeDNS, _updateData);
        var (result, msg) = await t.PerformUpdate();
        Assert.False(result);
    }
    
    
    [Fact]
    public async Task TestPerformUpdate_NoExistingDns()
    {
        var fakeDNS = new FakeDnsManagementClient() { ExistingIp = null };
        UpdateDnsARecord t = new UpdateDnsARecord(_fakeLogger, fakeDNS, _updateData);
        var (result, msg) = await t.PerformUpdate();
        Assert.True(result);
        Assert.Equal("good ip", msg);
        Assert.True(fakeDNS.CalledUpdate);
    }

    [Fact]
    public async Task TestPerformUpdate_NoChangeExpected()
    {
        var fakeDNS = new FakeDnsManagementClient() { ExistingIp = "1.2.3.4" };
        _updateData.reqip = "1.2.3.4";
        UpdateDnsARecord t = new UpdateDnsARecord(_fakeLogger, fakeDNS, _updateData);
        var (result, msg) = await t.PerformUpdate();
        Assert.True(result);
        Assert.Equal("nochg 1.2.3.4", msg);
        Assert.False(fakeDNS.CalledUpdate);
        Assert.False(fakeDNS.CalledAdd);
    }
    
    [Fact]
    public async Task TestPerformUpdate_ChangedIp()
    {
        var fakeDNS = new FakeDnsManagementClient() { ExistingIp = "1.2.3.4" };
        _updateData.reqip = "4.3.2.1";
        UpdateDnsARecord t = new UpdateDnsARecord(_fakeLogger, fakeDNS, _updateData);
        var (result, msg) = await t.PerformUpdate();
        Assert.True(result);
        Assert.Equal("good 4.3.2.1", msg);
        Assert.False(fakeDNS.CalledUpdate);
        Assert.True(fakeDNS.CalledAdd);
    }

}