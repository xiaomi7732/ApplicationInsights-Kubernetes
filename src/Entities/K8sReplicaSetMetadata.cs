﻿using Newtonsoft.Json;

namespace Microsoft.ApplicationInsights.Kubernetes.Entities
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class K8sReplicaSetMetadata : K8sObjectMetadata
    {
    }
}
