using System;
using Microsoft.ApplicationInsights.Kubernetes.Entities;
using Microsoft.Extensions.Logging;

using static Microsoft.ApplicationInsights.Kubernetes.StringUtils;

namespace Microsoft.ApplicationInsights.Kubernetes
{
    internal class K8sPodWatcher : K8sWatcher<K8sPod>
    {
        public K8sPodWatcher(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        protected override string GetRelativePath(string queryNamespace)
        {
            return Invariant($"api/v1/namespaces/{queryNamespace}/pods?watch=true");
        }
    }
}