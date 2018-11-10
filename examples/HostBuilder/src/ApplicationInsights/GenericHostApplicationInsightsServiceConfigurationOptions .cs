using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.IO;
namespace HostBuilderExample.ApplicationInsights
{
    internal class GenericHostApplicationInsightsServiceConfigurationOptions : IConfigureOptions<ApplicationInsightsServiceOptions>
    {
        private readonly IHostingEnvironment hostingEnvironment;
        public GenericHostApplicationInsightsServiceConfigurationOptions(IHostingEnvironment hostingEnvironment)
        {
            this.hostingEnvironment = hostingEnvironment;
        }
        public void Configure(ApplicationInsightsServiceOptions options)
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .SetBasePath(this.hostingEnvironment.ContentRootPath ?? Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile(string.Format(CultureInfo.InvariantCulture, "appsettings.{0}.json", hostingEnvironment.EnvironmentName), true)
                .AddEnvironmentVariables();
            HostBuilderExample.ApplicationInsights.Extensions.AddTelemetryConfiguration(configBuilder.Build(), options);
            if (Debugger.IsAttached)
            {
                options.DeveloperMode = true;
            }
        }
    }
}