using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Dns;
using Azure.ResourceManager.Dns.Models;
using Azure.ResourceManager.Resources;
using AzureAppFunc.interfaces;

namespace AzureAppFunc.logic;

public class DnsManagement : IDnsManagement
{
    public async Task<DnsZoneResource?> GetArmDnsZone(DnsManagementData data)
    {
        ArmClient client = new ArmClient(new DefaultAzureCredential());
        SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroups().GetAsync(data.ResourceGroupName);
        DnsZoneCollection dnsZones = resourceGroup.Value.GetDnsZones();
        var zoneResponse = await dnsZones.GetAsync(data.ZoneName);
        if (!zoneResponse.HasValue)
            return null;
        return zoneResponse.Value;
    }
    
    public async Task<Tuple<bool, string>> UpdateDnsRecordSetAsync(DnsManagementData data)
    {
        try
        {
            if (!data.IsValid(out var msg))
            {
                return new Tuple<bool, string>(false, $"911 {msg}");
            }

            var zone = await GetArmDnsZone(data);
            if(null == zone)
            {
                return new Tuple<bool, string>(false, "911 dns zone not found");
            }

            // the IP address were using / setting.
            var theDnsARecord = new DnsARecordData()
            {
                TtlInSeconds = 3600,
                DnsARecords = { new DnsARecordInfo { IPv4Address = data.ValidatedIpv4Address } }
            };

            var recordCollection = zone.GetDnsARecords();
            
            // does the record already exist?
            var existingRecord = await recordCollection.ExistsAsync(data.RecordSetName);

            // it doesnt exist, create it  
            if (existingRecord.HasValue && existingRecord.Value == false)
            {
                ArmOperation<DnsARecordResource> aRecordOperation = await recordCollection.CreateOrUpdateAsync(WaitUntil.Completed, 
                    data.RecordSetName, theDnsARecord);
                return new Tuple<bool, string>(true, $"good {data.RequestedIpAddress}");
            }
            else
            {
                var aRecordResponse = await recordCollection.GetAsync(data.RecordSetName);
                var aRecord = aRecordResponse.Value;
                
                // it already exists, and is the same?
                if(aRecord.Data.Name == data.RecordSetName &&
                   aRecord.Data.DnsARecords.Any(x => x.IPv4Address.ToString() == data.ValidatedIpv4Address?.ToString()))
                {
                    Console.WriteLine($"No change required for DNS record: {data.RecordSetName}");
                    return new Tuple<bool, string>(true, $"nochg {data.RequestedIpAddress}");
                }
                
                // ok, it isn't there or it is there but different
                aRecord.Data.DnsARecords.Clear();
                aRecord.Data.DnsARecords.Add(new DnsARecordInfo { IPv4Address = data.ValidatedIpv4Address });
                await aRecord.UpdateAsync(aRecord.Data);
                return new Tuple<bool, string>(true, $"good {data.RequestedIpAddress}");
            }
        }
        catch (RequestFailedException ex)
        {
            return new Tuple<bool, string>(false, $"azure error: {ex.Message}");
        }
    }

    public async Task<ArmOperation> DeleteDnsRecordSetAsync(DnsManagementData dnsManagementData)
    {
        var zone = await GetArmDnsZone(dnsManagementData);
        if (null == zone)
        {
            throw new Exception("the DNS zone was not found");
        }
        
        var recordCollection = zone.GetDnsARecords();
        var recordSetResponse = await recordCollection.GetAsync(dnsManagementData.RecordSetName);
        return await recordSetResponse.Value.DeleteAsync(WaitUntil.Completed);
    }
}