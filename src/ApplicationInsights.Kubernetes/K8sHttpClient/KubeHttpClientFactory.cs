﻿using Microsoft.ApplicationInsights.Kubernetes.Debugging;

namespace Microsoft.ApplicationInsights.Kubernetes
{
    internal class KubeHttpClientFactory
    {
        private readonly Inspect _logger = Inspect.Instance;

        public KubeHttpClientFactory()
        {
        }

        public IKubeHttpClient Create(IKubeHttpClientSettingsProvider settingsProvider)
        {
            _logger.LogTrace("Creating {0}", nameof(KubeHttpClient));
            return new KubeHttpClient(settingsProvider);
        }
    }
}
