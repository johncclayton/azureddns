using System;
using System.Threading.Tasks;
using AzureAppFunc.logic;

namespace AzureAppFunc.interfaces;

public interface IDnsManagement
{
    Task<Tuple<bool, string>> UpdateDnsRecordSetAsync(DnsManagementData data);
}