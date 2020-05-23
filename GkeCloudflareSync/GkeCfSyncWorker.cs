using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GkeCloudflareSync
{
    public class GkeCfSyncWorker : BackgroundService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<GkeCfSyncWorker> _logger;
        private readonly ICloudflareService _cloudflare;
        private readonly IKubernetesService _kubernetes;

        private readonly TimeSpan[] _retryPolicy;

        public GkeCfSyncWorker(
            IHostApplicationLifetime hostApplicationLifetime,
            ILogger<GkeCfSyncWorker> logger,
            ICloudflareService cloudflare,
            IKubernetesService kubernetes)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
            _cloudflare = cloudflare;
            _kubernetes = kubernetes;

            _retryPolicy = new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() =>
            {
                _logger.LogInformation("Application execution cancelled");
                _hostApplicationLifetime.StopApplication();
            });

            _logger.LogInformation("GkeCfSyncWorker started at: {time}", DateTimeOffset.Now);

            foreach (var delay in _retryPolicy)
            {
                try
                {
                    var ip = await _kubernetes.GetHostNodeExternalIp();
                    await _cloudflare.UpdateDnsARecord(ip);
                    _hostApplicationLifetime.StopApplication();
                }
                catch
                {
                    _logger.LogInformation($"Retrying in {delay.TotalSeconds:N0}s...");
                    await Task.Delay(delay);
                }
            }

            _logger.LogError("Sync operation failed");
        }
    }
}
