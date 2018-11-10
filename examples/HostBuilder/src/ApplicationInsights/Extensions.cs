using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WindowsServer;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Hosting = Microsoft.Extensions.Hosting;
using WebHosting = Microsoft.AspNetCore.Hosting;

namespace HostBuilderExample.ApplicationInsights
{
    public static class Extensions
    {
        /// <summary>
        /// Adds Application Insights services for .NET Core into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddGenericApplicationInsightsTelemetry(this IServiceCollection services)
        {
            services = services.InjectApplicationInsightsBasics();
            services.TryAddSingleton<IConfigureOptions<ApplicationInsightsServiceOptions>,
                   GenericHostApplicationInsightsServiceConfigurationOptions>();
            services.AddSingleton<TelemetryClient>();
            return services;
        }
        /// <summary>
        /// Adds Application Insights services for .NET Core into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="instrumentationKey">Instrumentation key to use for telemetry.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddGenericApplicationInsightsTelemetry(this IServiceCollection services, string instrumentationKey)
        {
            services.AddGenericApplicationInsightsTelemetry(options => options.InstrumentationKey = instrumentationKey);
            return services;
        }
        /// <summary>
        /// Adds Application Insights services for .NET Core into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="options">The action used to configure the options.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddGenericApplicationInsightsTelemetry(this IServiceCollection services,
                Action<ApplicationInsightsServiceOptions> options)
        {
            services.AddGenericApplicationInsightsTelemetry();
            services.Configure(options);
            return services;
        }
        private static IServiceCollection InjectApplicationInsightsBasics(this IServiceCollection services)
        {
            {
                // We treat ApplicationInsightsDebugLogger as a marker that AI services were added to service collection	            // Initializers
                services.AddSingleton<ITelemetryInitializer, AzureWebAppRoleEnvironmentTelemetryInitializer>();
                services.AddSingleton<ITelemetryInitializer, ComponentVersionTelemetryInitializer>();
                services.AddSingleton<ITelemetryInitializer,
                    Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers.DomainNameRoleInstanceTelemetryInitializer>();
                services.TryAddSingleton<ITelemetryChannel, ServerTelemetryChannel>();
                // Modules
                services.AddSingleton<ITelemetryModule, PerformanceCollectorModule>();
                services.AddSingleton<ITelemetryModule, AzureInstanceMetadataTelemetryModule>();
                services.AddSingleton<ITelemetryModule, QuickPulseTelemetryModule>();
                services.AddSingleton<TelemetryConfiguration>(provider =>
                   provider.GetService<IOptions<TelemetryConfiguration>>().Value);
                services.TryAddSingleton<IApplicationIdProvider, ApplicationInsightsApplicationIdProvider>();
                // Using startup filter instead of starting DiagnosticListeners directly because
                // AspNetCoreHostingDiagnosticListener injects TelemetryClient that injects TelemetryConfiguration
                // that requires IOptions infrastructure to run and initialize
                services.AddSingleton<IStartupFilter, ApplicationInsightsStartupFilter>();
                services.AddOptions();
                services.AddSingleton<IOptions<TelemetryConfiguration>, TelemetryConfigurationOptions>();
                services.AddSingleton<IConfigureOptions<TelemetryConfiguration>, TelemetryConfigurationOptionsSetup>();
                return services;
            }
        }

        /// <summary>
        /// Read from configuration
        /// Config.json will look like this:
        /// <para>
        ///      "ApplicationInsights": {
        ///          "InstrumentationKey": "11111111-2222-3333-4444-555555555555"
        ///          "TelemetryChannel": {
        ///              EndpointAddress: "http://dc.services.visualstudio.com/v2/track",
        ///              DeveloperMode: true
        ///          }
        ///      }
        /// </para>
        /// Values can also be read from environment variables to support azure web sites configuration:
        /// </summary>
        /// <param name="config">Configuration to read variables from.</param>
        /// <param name="serviceOptions">Telemetry configuration to populate.</param>
        internal static void AddTelemetryConfiguration(IConfiguration config,
            ApplicationInsightsServiceOptions serviceOptions)
        {
            string instrumentationKey = config[InstrumentationKeyForWebSites];
            if (string.IsNullOrWhiteSpace(instrumentationKey))
            {
                instrumentationKey = config[InstrumentationKeyFromConfig];
            }

            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                serviceOptions.InstrumentationKey = instrumentationKey;
            }

            string developerModeValue = config[DeveloperModeForWebSites];
            if (string.IsNullOrWhiteSpace(developerModeValue))
            {
                developerModeValue = config[DeveloperModeFromConfig];
            }

            if (!string.IsNullOrWhiteSpace(developerModeValue))
            {
                bool developerMode = false;
                if (bool.TryParse(developerModeValue, out developerMode))
                {
                    serviceOptions.DeveloperMode = developerMode;
                }
            }

            string endpointAddress = config[EndpointAddressForWebSites];
            if (string.IsNullOrWhiteSpace(endpointAddress))
            {
                endpointAddress = config[EndpointAddressFromConfig];
            }

            if (!string.IsNullOrWhiteSpace(endpointAddress))
            {
                serviceOptions.EndpointAddress = endpointAddress;
            }

            var version = config[VersionKeyFromConfig];
            if (!string.IsNullOrWhiteSpace(version))
            {
                serviceOptions.ApplicationVersion = version;
            }
        }

        private const string VersionKeyFromConfig = "version";
        private const string InstrumentationKeyFromConfig = "ApplicationInsights:InstrumentationKey";
        private const string DeveloperModeFromConfig = "ApplicationInsights:TelemetryChannel:DeveloperMode";
        private const string EndpointAddressFromConfig = "ApplicationInsights:TelemetryChannel:EndpointAddress";

        private const string InstrumentationKeyForWebSites = "APPINSIGHTS_INSTRUMENTATIONKEY";
        private const string DeveloperModeForWebSites = "APPINSIGHTS_DEVELOPER_MODE";
        private const string EndpointAddressForWebSites = "APPINSIGHTS_ENDPOINTADDRESS";
    }
}