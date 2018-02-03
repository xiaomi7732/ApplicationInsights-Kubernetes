namespace Microsoft.ApplicationInsights.Kubernetes
{
    internal interface IK8sWatchable
    {
        string WatchId { get; }
        string ResourceVersion { get; }
        string ResourceName { get; }
    }
}