using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using HostBuilderExample.ApplicationInsights;

namespace HostBuilderExample
{
    class Program
    {
        static void Main(string[] args)
        {

            IHost host = new HostBuilder().ConfigureHostConfiguration(configHost =>
            {
                configHost.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
                // Althrough we can customize where the hostsettings, due to current limitation, 
                // application insights settings has to be put in appsettings.json / appsettings.{environemntName}.json. Reference:
                // https://github.com/Microsoft/ApplicationInsights-aspnetcore/blob/04b5485d4a8aa498b2d99c60bdf8ca59bc9103fc/src/Microsoft.ApplicationInsights.AspNetCore/Extensions/DefaultApplicationInsightsServiceConfigureOptions.cs#L31
                configHost.AddJsonFile("hostsettings.json", optional: false);
                configHost.AddEnvironmentVariables();
            }).ConfigureServices((hostContext, services) =>
            {
                // Enable console logging for ILogger
                services.AddLogging(cfg => { cfg.AddConsole(); });
                // Microsoft.AspNetCore.Hosting.IHostingEnvironment is required by Application Insights SDK
                services.AddGenericApplicationInsightsTelemetry();
                // Enable Application Insights for Kubernetes.
                services.EnableKubernetes();

                // Inject the service.
                services.AddSingleton<IHostedService, PrintHelloService>();
            })
            .UseConsoleLifetime()
            .Build();

            host.Run();
        }
    }
}
