namespace Microsoft.ApplicationInsights.Kubernetes.Entities
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class K8sPod : K8sObject, IK8sWatchable
    {
        [JsonProperty("metadata")]
        public K8sPodMetadata Metadata { get; set; }

        [JsonProperty("status")]
        public K8sPodStatus Status { get; set; }

        [JsonProperty("spec")]
        public K8sPodSpec Spec { get; set; }

        public string WatchId => Metadata.Uid;

        public string ResourceVersion => Metadata.ResourceVersion;

        public string ResourceName => Metadata.Name;
    }
}
