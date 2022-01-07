using System;

using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;

using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Rest;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace azureddns
{

	/// <summary>
	/// Summary description for Class1
	/// </summary>
	public class UpdateDNS_ARecord
	{
        private DnsManagementClient _client = null;
        private ILogger _log;
        private UpdateData _data;

        public UpdateDNS_ARecord(ILogger log, DnsManagementClient client, UpdateData data)
		{
            _log = log;
            _data = data;
            _client = client;
        }

        public async Task<Tuple<bool, string>> PerformUpdate()
        {
            RecordSet recordSet = null;

            if (string.IsNullOrEmpty(_client.SubscriptionId))
            {
                return new Tuple<bool, string>(false, "subscription ID could not be found, this means auth will fail for dns code - stopping here");
            }

            try
            {
                _log.LogInformation($"looking for A record in group: {_data.group}, zone: {_data.zone}, name: {_data.name}");
                recordSet = await _client.RecordSets.GetAsync(_data.group, _data.zone, _data.name, RecordType.A);
            }

            catch { }

            if (recordSet != null)
            {
                _log.LogInformation($"found one, I'll overwrite the the list of IPs with this: {_data.reqip}");

                // if this is exactly the same IP addreess then we can return a diff. result code
                if (recordSet.ARecords.Count == 1 && recordSet.ARecords[0].Ipv4Address == _data.reqip)
                    return new Tuple<bool, string>(true, $"nochg {_data.reqip}");

                recordSet.ARecords.Clear();
                recordSet.ARecords.Add(new ARecord(_data.reqip));
                recordSet = await _client.RecordSets.UpdateAsync(_data.group, _data.zone, _data.name, RecordType.A, recordSet);

                return new Tuple<bool, string>(true, $"good {_data.reqip}");
            }
            else
            {
                _log.LogInformation($"creating an ARecord for name: {_data.name}, IP: {_data.reqip}");

                recordSet = new RecordSet();

                recordSet.TTL = 3600;
                recordSet.ARecords = new List<ARecord>();
                recordSet.ARecords.Add(new ARecord(_data.reqip));
                recordSet = await _client.RecordSets.CreateOrUpdateAsync(_data.group, _data.zone, _data.name, RecordType.A, recordSet);

                return new Tuple<bool, string>(true, $"good {_data.reqip}");
            }

            
        }
    }

}