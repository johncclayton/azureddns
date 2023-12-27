using System;
using System.Threading.Tasks;

namespace AzureAppFunc.interfaces;

public interface IDnsManagement
{
    Task<Tuple<bool, string>> UpdateDnsRecordSetAsync();
}