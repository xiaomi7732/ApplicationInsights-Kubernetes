using Newtonsoft.Json;

namespace Microsoft.ApplicationInsights.Kubernetes.Entities
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class K8sWatchableObjectMetadata : K8sObjectMetadata
    {
        [JsonProperty("resourceVersion")]
        public string ResourceVersion { get; set; }
    }
}