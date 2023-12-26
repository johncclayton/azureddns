using System;
using System.Net;

namespace AzureAppFunc.logic;

public class DnsManagementData
{
    public string ZoneName, RecordSetName, ResourceGroupName, RequestedIpAddress;
    public readonly IPAddress? ValidatedIpv4Address;

    public DnsManagementData(string zoneName, string recordSetName, string resourceGroupName, string requestedIpAddress)
    {
        this.ZoneName = zoneName;
        this.RecordSetName = recordSetName;
        this.ResourceGroupName = resourceGroupName;
        this.RequestedIpAddress = requestedIpAddress;

        IPAddress.TryParse(requestedIpAddress, out ValidatedIpv4Address);
    }

    public virtual bool IsValid(out string msg)
    {
        if (string.IsNullOrEmpty(RecordSetName))
        {
            msg = new string("no 'recordsetname' value, use this to specify the recordset / hostname");
            return false;
        }

        if (string.IsNullOrEmpty(ResourceGroupName))
        {
            msg = new string("no 'resourcegroup', please specify a resource group where the zone exists");
            return false;
        }

        if (string.IsNullOrEmpty(ZoneName))
        {
            msg = new string("no 'zonename', specify the DNS zone - it must exist already");
            return false;
        }

        if (string.IsNullOrEmpty(RequestedIpAddress))
        {
            msg = new string("no requested IP address has been set");
            return false;
        }

        if (null == ValidatedIpv4Address)
        {
            msg = new string($"IP address value provided ({RequestedIpAddress}, but it didn't validate");
            return false;
        }

        msg = String.Empty;

        return true;
    }
}