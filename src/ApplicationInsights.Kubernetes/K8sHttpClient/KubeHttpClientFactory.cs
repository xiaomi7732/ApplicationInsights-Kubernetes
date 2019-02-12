using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Kubernetes
{
    internal class KubeHttpClientFactory
    {
        private readonly ILogger _logger;
        private readonly ILogger<IKubeHttpClient> _httpClientLogger;

        public KubeHttpClientFactory(ILogger<KubeHttpClientFactory> logger, ILogger<IKubeHttpClient> httpClientLogger)
        {
            _logger = Arguments.IsNotNull(logger, nameof(logger));
            _httpClientLogger = Arguments.IsNotNull(httpClientLogger, nameof(httpClientLogger));
        }

        public IKubeHttpClient Create(IKubeHttpClientSettingsProvider settingsProvider)
        {
            _logger.LogTrace($"Creating {nameof(KubeHttpClient)}");
            return new KubeHttpClient(settingsProvider, _httpClientLogger);
        }

    }
}
