using k8s;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GkeCloudflareSync
{
    public interface IKubernetesService
    {
        Task<string> GetHostNodeExternalIp();
    }

    public class KubernetesService : IKubernetesService
    {
        private readonly ILogger<KubernetesService> _logger;
        private readonly AppConfiguration _config;

        public KubernetesService(ILogger<KubernetesService> logger, IOptions<AppConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task<string> GetHostNodeExternalIp()
        {
            try
            {
                _logger.LogDebug("Loading k8s in cluster config...");
                var config = KubernetesClientConfiguration.InClusterConfig();

                _logger.LogDebug("Init k8s client...");
                var client = new Kubernetes(config);

                _logger.LogDebug("Listing pods for all namespaces...");
                var pods = await client.ListPodForAllNamespacesAsync();
                _logger.LogInformation($"Found {pods?.Items?.Count} pods for all namespaces");

                _logger.LogDebug($"Locating host pod '{_config.Hostname}' ...");
                var hostPod = pods.Items.FirstOrDefault(x => x.Metadata.Name == _config.Hostname);
                var hostIp = hostPod?.Status?.HostIP;
                if (hostPod == null)
                {
                    throw new Exception("Unable to found host pod");
                }
                if (hostIp == null)
                {
                    throw new Exception("Unable to get host ip from pod status");
                }
                _logger.LogInformation($"Found host pod '{_config.Hostname}' with host ip '{hostIp}'");

                _logger.LogDebug("Listing nodes...");
                var nodes = await client.ListNodeAsync();
                _logger.LogInformation($"Found {nodes?.Items?.Count} nodes");

                _logger.LogDebug($"Locating host node...");
                var hostNode = nodes.Items.FirstOrDefault(x => x.Status.Addresses.Any(y => y.Address == hostIp));
                var hostNodeExternalIp = hostNode?.Status?.Addresses.FirstOrDefault(x => x.Type == _config.ExternalIpNodeAddressType)?.Address;
                if (hostNode == null)
                {
                    throw new Exception("Unable to found host node");
                }
                if (hostNodeExternalIp == null)
                {
                    throw new Exception($"Unable to get {_config.ExternalIpNodeAddressType} from host node");
                }
                _logger.LogInformation($"External ip for host node is '{hostNodeExternalIp}'");

                return hostNodeExternalIp;
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Error while getting host node external ip");
                throw;
            }
        }
    }
}
