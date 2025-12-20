using System.Collections.Generic;
using System.Linq;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Dns;
using Azure.ResourceManager.Resources;
using static System.String;

namespace AzureAppFunc.logic;

public class DnsManagementZone
{
    public string ZoneName, ResourceGroupName;
    public List<DnsManagementRecordSet> ChildRecordSet = new List<DnsManagementRecordSet>();

    private DnsZoneResource? _zoneValue;
    public DnsZoneResource? Zone => _zoneValue ??= GetArmDnsZone();

    public DnsManagementZone(string zoneName, string resourceGroupName)
    {
        ZoneName = zoneName;
        ResourceGroupName = resourceGroupName;
    }
    
    private DnsZoneResource? GetArmDnsZone()
    {
        ArmClient client = new ArmClient(new DefaultAzureCredential());
        SubscriptionResource subscription = client.GetDefaultSubscriptionAsync().GetAwaiter().GetResult();
        var resourceGroup = subscription.GetResourceGroups().GetAsync(ResourceGroupName).GetAwaiter().GetResult();
        DnsZoneCollection dnsZones = resourceGroup.Value.GetDnsZones();
        var zoneResponse = dnsZones.GetAsync(ZoneName).GetAwaiter().GetResult();
        if (!zoneResponse.HasValue)
            return null;
        return zoneResponse.Value;
    }

    public static DnsManagementZone TryParse(string zoneName, string resGroup, string recordSetName, string requestedIp)
    {
        DnsManagementZone newZone = new DnsManagementZone(zoneName, resGroup);
        var splitRecordSetName = recordSetName.Split(',');
        
        foreach (var name in splitRecordSetName)
        {
            var strippedName = name.Trim();
            if (IsNullOrEmpty(strippedName))
                continue;

            DnsManagementRecordSet rs = new DnsManagementRecordSet(newZone, strippedName, requestedIp);
            newZone.AddRecordSet(rs);
        }

        return newZone;
    }

    private void AddRecordSet(DnsManagementRecordSet rs)
    {
        ChildRecordSet.Add(rs);
    }
}