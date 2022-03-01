using System.Threading.Tasks;
using azureddns.interfaces;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.Core;
using Microsoft.Rest;
using Microsoft.Azure.Management.Dns;

namespace azureddns;

public class AzureDnsManagementClient : IDNSManagementClient
{
    private readonly DnsManagementClient _client;

    public AzureDnsManagementClient(DnsManagementClient c)
    {
        _client = c;
    }

    public bool IsValid()
    {
        return string.IsNullOrEmpty(_client.SubscriptionId) == false;
    }

    public Task<RecordSet> GetRecordSetAsync(string resgroup, string zone, string name, RecordType type)
    {
        return _client.RecordSets.GetAsync(resgroup, zone, name, type);
    }

    public Task<RecordSet> AddRecordSetAsync(string resgroup, string zone, string name, RecordType recordType,
        RecordSet recordSet)
    {
        return _client.RecordSets.UpdateAsync(resgroup, zone, name, recordType, recordSet);

    }

    public Task<RecordSet> CreateOrUpdateRecordSetAsync(string resgroup, string zone, string name, RecordType recordType,
        RecordSet recordSet)
    {
        return _client.RecordSets.CreateOrUpdateAsync(resgroup, zone, name, recordType, recordSet);
    }
}