using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace GkeCloudflareSync
{
    public interface ICloudflareClient
    {
        Task<string> GetDnsARecordId(string zoneId, string domain);
        Task<string> GetZoneId(string domain);
        Task UpdateDnsARecord(string zoneId, string dnsRecordId, string dnsRecordContent);
    }

    public class CloudflareClient : ICloudflareClient
    {
        private readonly HttpClient _httpClient;
        private readonly AppConfiguration _config;

        public CloudflareClient(HttpClient client, IOptions<AppConfiguration> config)
        {
            _httpClient = client;
            _config = config.Value;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _config.CloudflareApiToken);
        }

        public async Task<string> GetZoneId(string domain)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("name", domain);
            query.Add("status", "active");
            query.Add("page", "1");
            query.Add("per_page", "20");
            query.Add("order", "status");
            query.Add("direction", "desc");
            query.Add("match", "all");

            var url = new UriBuilder(_httpClient.BaseAddress)
            {
                Port = -1,
                Path = $"client/v4/zones",
                Query = query.ToString(),
            };

            var request = new HttpRequestMessage(HttpMethod.Get, url.ToString());
            var response = await _httpClient.SendAsync(request);
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Unable to get zone details from Cloudflare API. Json response: {jsonContent}");
            }

            var content = JObject.Parse(jsonContent);
            var zoneId = content.SelectToken("$.result[0].id")?.Value<string>();

            if (string.IsNullOrEmpty(zoneId))
            {
                throw new Exception($"Unable to read zone id from Cloudflare API response. Json response: {jsonContent}");
            }

            return zoneId;
        }

        public async Task<string> GetDnsARecordId(string zoneId, string domain)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("type", "A");
            query.Add("name", domain);
            query.Add("page", "1");
            query.Add("per_page", "20");
            query.Add("order", "type");
            query.Add("direction", "desc");
            query.Add("match", "all");

            var url = new UriBuilder(_httpClient.BaseAddress)
            {
                Port = -1,
                Path = $"client/v4/zones/{zoneId}/dns_records",
                Query = query.ToString(),
            };

            var request = new HttpRequestMessage(HttpMethod.Get, url.ToString());
            var response = await _httpClient.SendAsync(request);
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Unable to get dns records from Cloudflare API. Json response: {jsonContent}");
            }

            var content = JObject.Parse(jsonContent);
            var dnsRecordId = content.SelectToken("$.result[0].id")?.Value<string>();

            if (string.IsNullOrEmpty(dnsRecordId))
            {
                throw new Exception($"Unable to read dns record id from Cloudflare API response. Json response: {jsonContent}");
            }

            return dnsRecordId;
        }

        public async Task UpdateDnsARecord(string zoneId, string dnsRecordId, string dnsRecordContent)
        {
            var url = new UriBuilder(_httpClient.BaseAddress)
            {
                Port = -1,
                Path = $"client/v4/zones/{zoneId}/dns_records/{dnsRecordId}",
            };

            var body = JsonConvert.SerializeObject(new
            {
                type = "A",
                name = _config.Domain,
                content = dnsRecordContent,
                ttl = "1",
                proxied = true,
            });

            var request = new HttpRequestMessage(HttpMethod.Put, url.ToString());
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Unable to get dns records from Cloudflare API. Json response: {jsonContent}");
            }
        }
    }
}
