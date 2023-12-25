using System.Threading.Tasks;
using Microsoft.Azure.Management.Dns.Models;

namespace AzureAppFunc.interfaces;

public interface IDnsManagementClient
{
    bool IsValid();
    
    Task<RecordSet?> GetRecordSetAsync(string resgroup, string zone, string name, RecordType type);
    Task<RecordSet?> AddRecordSetAsync(string dataResgroup, string dataZone, string dataName, RecordType recordType, RecordSet recordSet);
    Task<RecordSet?> CreateOrUpdateRecordSetAsync(string dataResgroup, string dataZone, string dataName, RecordType recordType, RecordSet recordSet);
}