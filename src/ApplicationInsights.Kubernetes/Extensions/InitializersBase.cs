using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace OpenTelemetries.Instrumentations.Kubernetes.Extensions;

internal abstract class InitializersBase : IHostedLifecycleService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public abstract Task StartingAsync(CancellationToken cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}