using System;
using Microsoft.ApplicationInsights.Kubernetes;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Object model of configuration for Application Insights for Kubernetes.
    /// </summary>
    public class AppInsightsForKubernetesOptions
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string SectionName = "AppInsightsForKubernetes";

        /// <summary>
        /// Maximum time to wait for the clsuter info to become available.
        /// </summary>
        public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets an environment check action to determine if the the current process is inside a Kubernetes cluster.
        /// When set to null (also the default), a built-in checker will be used.
        /// </summary>
        public IClusterEnvironmentCheck? ClusterCheckAction { get; set; } = null;
    }
}
