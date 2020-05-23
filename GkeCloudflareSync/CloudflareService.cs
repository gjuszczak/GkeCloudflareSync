using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace GkeCloudflareSync
{
    public interface ICloudflareService
    {
        Task UpdateDnsARecord(string ip);
    }

    public class CloudflareService : ICloudflareService
    {
        private readonly ICloudflareClient _client;
        private readonly ILogger<CloudflareService> _logger;
        private readonly AppConfiguration _config;

        public CloudflareService(ICloudflareClient client, ILogger<CloudflareService> logger, IOptions<AppConfiguration> config)
        {
            _client = client;
            _logger = logger;
            _config = config.Value;
        }

        public async Task UpdateDnsARecord(string ip)
        {
            try
            {
                _logger.LogDebug($"Locating Cloudflare zone id for domain '{_config.Domain}'...");
                var zoneId = await _client.GetZoneId(_config.Domain);

                _logger.LogDebug($"Locating Cloudflare DNS A record id for zone id '{zoneId}' and domain '{_config.Domain}'...");
                var dnsRecordId = await _client.GetDnsARecordId(zoneId, _config.Domain);

                _logger.LogDebug($"Updating Cloudflare DNS A record '{dnsRecordId}' for zone id '{zoneId}' using ip value '{ip}'...");
                await _client.UpdateDnsARecord(zoneId, dnsRecordId, ip);
                _logger.LogInformation($"Cloudflare DNS A record for domain '{_config.Domain}' updated successfully to ip '{ip}'");

            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Error while updating DNS A record in Cloudflare");
                throw;
            }
        }
    }
}
