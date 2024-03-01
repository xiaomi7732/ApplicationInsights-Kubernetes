using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Kubernetes;
using Microsoft.Extensions.DependencyInjection;

namespace OpenTelemetries.Instrumentations.Kubernetes.Extensions;

internal class K8sInfoInitializer : InitializersBase
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public K8sInfoInitializer(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new System.ArgumentNullException(nameof(serviceScopeFactory));
    }

    public override async Task StartingAsync(CancellationToken cancellationToken)
    {
        await using(AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope())
        {
            IK8sInfoBootstrap bootstrap = scope.ServiceProvider.GetRequiredService<IK8sInfoBootstrap>();
            await bootstrap.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}