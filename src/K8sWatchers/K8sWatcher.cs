namespace Microsoft.ApplicationInsights.Kubernetes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Kubernetes.Entities;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    using static Microsoft.ApplicationInsights.Kubernetes.StringUtils;

    internal abstract class K8sWatcher<TEntity> : IDisposable
        where TEntity : K8sObject, IK8sWatchable
    {
        private ILogger<K8sWatcher<TEntity>> logger;
        CancellationTokenSource cancellationTokenSource;
        bool isDisposed = false;

        public K8sWatcher(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory?.CreateLogger<K8sWatcher<TEntity>>();
            this.VersionTracker = new Dictionary<string, string>();
        }

        private Dictionary<string, string> VersionTracker { get; set; }

        public event EventHandler<K8sWatcherEventArgs> Changed;

        protected virtual void OnChanged(object sender, K8sWatcherEventArgs e)
        {
            Changed?.Invoke(sender, e);
        }

        protected abstract string GetRelativePath(string queryNamespace);

        public virtual async Task StartWatchAsync(IKubeHttpClient kubeHttpClient)
        {
            Arguments.IsNotNull(kubeHttpClient, nameof(kubeHttpClient));
            this.cancellationTokenSource = new CancellationTokenSource();

            string line = null;
            using (Stream inStream = await kubeHttpClient.GetStreamAsync(kubeHttpClient.GetQueryUrl(this.GetRelativePath(kubeHttpClient.Settings.QueryNamespace))).ConfigureAwait(false))
            using (StreamReader reader = new StreamReader(inStream))
            {
                try
                {
                    line = await reader.ReadLineAsync().ConfigureAwait(false);
                    while (!string.IsNullOrEmpty(line))
                    {
                        if (cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                        Process(line);
                        line = await reader.ReadLineAsync().ConfigureAwait(false);
                    }
                    logger?.LogWarning("Should never run into this spot unless exception happened.");
                }
                catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    // Stop is called.
                    logger?.LogDebug($"Stop is called. Details: {ex.Message}");
                }
            }
        }

        private void Process(string jsonLine)
        {
            try
            {
                K8sWatcherObject<TEntity> watcherObject = JsonConvert.DeserializeObject<K8sWatcherObject<TEntity>>(jsonLine);
                string key = watcherObject?.EventObject?.WatchId;
                string value = watcherObject?.EventObject?.ResourceVersion;

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    bool eventHappens = false;
                    if (this.VersionTracker.ContainsKey(key))
                    {
                        if (this.VersionTracker[key] != value)
                        {
                            // Event happens
                            eventHappens = true;
                        }
                    }
                    else
                    {
                        // Newly found object
                        eventHappens = true;
                    }

                    if (eventHappens)
                    {
                        // Raise the event and recornd the last version.
                        this.OnChanged(this, GetK8sWatcherEventArgs(watcherObject));
                        this.VersionTracker[key] = value;
                        logger?.LogTrace(Invariant($"Object [{watcherObject?.EventObject?.WatchId})] of kind [{watcherObject?.EventObject?.Kind}] changed. Event type: {watcherObject?.EventType}"));
                    }
                }
                else
                {
                    logger?.LogDebug(Invariant($"Watch object key or value is null: {key} = {value}"));
                }
            }
            catch (JsonSerializationException ex)
            {
                logger?.LogTrace(ex.Message);
                logger?.LogError(jsonLine);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex.ToString());
            }
        }

        private K8sWatcherEventArgs GetK8sWatcherEventArgs(K8sWatcherObject<TEntity> watcherObject)
        {
            K8sWatcherEventArgs newArgs = new K8sWatcherEventArgs()
            {
                EventType = watcherObject.EventType,
                Entity = watcherObject.EventObject,
                ObjectUid = watcherObject.EventObject?.WatchId,
                ObjectKind = watcherObject?.EventObject.Kind,
                ObjectName = watcherObject?.EventObject?.ResourceName,
            };

            return newArgs;
        }

        public virtual void StopWatch()
        {
            this.cancellationTokenSource?.Cancel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (!this.isDisposed)
                {
                    this.isDisposed = true;

                    if (this.cancellationTokenSource != null)
                    {
                        this.cancellationTokenSource.Dispose();
                        this.cancellationTokenSource = null;
                    }
                }
            }
        }
    }
}