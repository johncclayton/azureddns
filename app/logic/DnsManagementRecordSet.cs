using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Dns;
using Azure.ResourceManager.Dns.Models;
using AzureAppFunc.interfaces;
using Microsoft.AspNetCore.Http.Internal;

namespace AzureAppFunc.logic;

public class DnsManagementRecordSet : IDnsManagement
{
    public readonly DnsManagementZone ParentZone;
    public readonly string RecordSetName;
    
    private string? _requestedIpAddress;
    private IPAddress? _validatedIpv4Address;
    
    public string? RequestedIpAddress
    {
        get => _requestedIpAddress;
        set
        {
            _requestedIpAddress = value;
            IPAddress.TryParse(_requestedIpAddress, out _validatedIpv4Address);    
        }
    }

    public DnsManagementRecordSet(DnsManagementZone newZone, string recordSetName, string? requestedIpAddress)
    {
        ParentZone = newZone;
        RecordSetName = recordSetName;
        RequestedIpAddress = requestedIpAddress; 
    }
    
    public virtual Tuple<bool, string> IsValid()
    {
        if (string.IsNullOrEmpty(RecordSetName))
        {
            return new (false, "no 'recordsetname' value, use this to specify the recordset / hostname");
        }

        if (string.IsNullOrEmpty(ParentZone.ResourceGroupName))
        {
            return new (false, "no 'resourcegroup', please specify a resource group where the zone exists");
        }

        if (string.IsNullOrEmpty(ParentZone.ZoneName))
        {
            return new (false, "no 'zonename', specify the DNS zone - it must exist already");
        }

        if (string.IsNullOrEmpty(_requestedIpAddress))
        {
            return new(false, "the requested IP addresses has not been set");
        }
        
        return new (true, string.Empty);
    }

    public async Task<Tuple<bool, string>> UpdateDnsRecordSetAsync()
    {
        var valid = IsValid();
        if (!valid.Item1)
        {
            return new Tuple<bool, string>(false, $"911 {valid.Item2}");
        }

        if (null == ParentZone.Zone)
        {
            return new Tuple<bool, string>(false, "911 dns zone not found");
        }
        
        try
        {
            var recordCollection = ParentZone.Zone.GetDnsARecords();

            // the IP address were using / setting, expressed as a DnsARecordData
            var theDnsARecord = new DnsARecordData()
            {
                TtlInSeconds = 3600,
                DnsARecords = { new DnsARecordInfo { IPv4Address = _validatedIpv4Address } }
            };

            // does the record already exist in the zone?
            var existingRecord = await recordCollection.ExistsAsync(RecordSetName);

            // if it doesnt exist, create it  
            if (existingRecord.HasValue && existingRecord.Value == false)
            {
                ArmOperation<DnsARecordResource> aRecordOperation = await recordCollection.CreateOrUpdateAsync(
                    WaitUntil.Completed,
                    RecordSetName, theDnsARecord);
                return new Tuple<bool, string>(true, $"good {RequestedIpAddress}");
            }
            else
            {
                // fetch the existing value, we need to check if the IP address is the same as what we want to set
                var aRecordResponse = await recordCollection.GetAsync(RecordSetName);
                var aRecord = aRecordResponse.Value;

                // and is it the same IP address?
                if (aRecord.Data.Name == RecordSetName &&
                    aRecord.Data.DnsARecords.Any(x =>
                        x.IPv4Address.ToString() == _validatedIpv4Address?.ToString()))
                {
                    Console.WriteLine($"No change required for DNS record: {RecordSetName}");
                    return new Tuple<bool, string>(true, $"nochg {RequestedIpAddress}");
                }

                // ok, it isn't there or it is there but has a different IP value, so update it
                aRecord.Data.DnsARecords.Clear();
                aRecord.Data.DnsARecords.Add(new DnsARecordInfo { IPv4Address = _validatedIpv4Address });
                await aRecord.UpdateAsync(aRecord.Data);
                
                return new Tuple<bool, string>(true, $"good {RequestedIpAddress}");
            }
        }
        catch (RequestFailedException ex)
        {
            return new Tuple<bool, string>(false, $"azure error: {ex.Message}");
        }
    }

    public async Task<ArmOperation> DeleteDnsRecordSetAsync()
    {
        if (null == ParentZone.Zone)
        {
            throw new Exception("the DNS zone was not found");
        }
        
        var recordCollection = ParentZone.Zone.GetDnsARecords();
        var recordSetResponse = await recordCollection.GetAsync(RecordSetName);
        
        return await recordSetResponse.Value.DeleteAsync(WaitUntil.Completed);
    }
}