using System.Collections.Generic;
using System.Threading.Tasks;
using AzureAppFunc.interfaces;
using Microsoft.Azure.Management.Dns.Models;

namespace testazureddns;

public class FakeDnsManagementClient : IDnsManagementClient
{
    public bool Valid { get; set; } = true;
    public string ResGroup { get; set; } = "group";
    public string Zone { get; set; } = "zone";
    public string Name { get; set; } = "name";
    public string? ExistingIp { get; set; }

    public bool CalledAdd { get; protected set; } = false;
    public bool CalledUpdate { get; protected set; } = false;
    
    public bool IsValid()
    {
        return Valid;
    }

    public async Task<RecordSet?> GetRecordSetAsync(string resgroup, string zone, string name, RecordType type)
    {
        if (null == ExistingIp)
            return await Task.FromResult<RecordSet?>(null);

        if (ExistingIp.Length == 0)
            return new RecordSet();
        
        RecordSet r = new RecordSet
        {
            ARecords = new List<ARecord> { new(ExistingIp) }
        };

        await Task.Delay(0);
        
        return r;
    }

    public Task<RecordSet?> AddRecordSetAsync(string dataResgroup, string dataZone, string dataName, RecordType recordType,
        RecordSet recordSet)
    {
        CalledAdd = true;
        return Task.FromResult<RecordSet?>(null);
    }

    public Task<RecordSet?> CreateOrUpdateRecordSetAsync(string dataResgroup, string dataZone, string dataName, RecordType recordType,
        RecordSet recordSet)
    {
        CalledUpdate = true;
        return Task.FromResult<RecordSet?>(null);
    }
}