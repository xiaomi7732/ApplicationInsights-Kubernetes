using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Kubernetes;

/// <summary>
/// A service to fetch kubernetes information for consumption.
/// The intention is for the client to have a handle to start getting Kubernetes info to be consumed by the <see cref="IK8sInfoService" />.
/// </summary>
public interface IK8sInfoBootstrap
{
    /// <summary>
    /// Bootstrap the fetch of Kubernetes information.
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken);
}
