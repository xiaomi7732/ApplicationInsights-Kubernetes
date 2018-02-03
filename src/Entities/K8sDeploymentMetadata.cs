using Newtonsoft.Json;

namespace Microsoft.ApplicationInsights.Kubernetes.Entities
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class K8sDeploymentMetadata : K8sObjectMetadata
    {

    }
}
