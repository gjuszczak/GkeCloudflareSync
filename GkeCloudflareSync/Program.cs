using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace GkeCloudflareSync
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var config = hostContext.Configuration;
                    services.Configure<AppConfiguration>(config);
                    services.AddHttpClient<ICloudflareClient, CloudflareClient>(c =>
                    {
                        c.BaseAddress = new Uri("https://api.cloudflare.com/");
                    });
                    services.AddTransient<ICloudflareService, CloudflareService>();
                    services.AddTransient<IKubernetesService, KubernetesService>();
                    services.AddHostedService<GkeCfSyncWorker>();
                });
    }
}
