using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Kubernetes.Entities;
using Microsoft.Extensions.Logging;

using static Microsoft.ApplicationInsights.Kubernetes.StringUtils;

namespace Microsoft.ApplicationInsights.Kubernetes
{

    /// <summary>
    /// Flatten objects for application insights or other external caller to fetch K8s properties.
    /// </summary>
    internal class K8sEnvironment : IK8sEnvironment, IDisposable
    {
        public static K8sEnvironment Current { get; private set; }

        // Property holder objects
        private K8sPod myPod;
        private ContainerStatus myContainerStatus;
        private K8sReplicaSet myReplicaSet;
        private K8sDeployment myDeployment;
        private K8sNode myNode;
        private ILogger<K8sEnvironment> logger;

        private K8sWatcher<K8sPod> podReadyWatcher;
        private KubeHttpClient httpClient;
        private KubeHttpClientSettingsProvider settings;

        // Waiter to making sure initialization code is run before calling into properties.
        private EventWaitHandle InitializationWaiter { get; set; }

        /// <summary>
        /// Private ctor to prevent the ctor being called.
        /// </summary>
#pragma warning disable CA2222 // Do not decrease inherited member visibility
        private K8sEnvironment()
#pragma warning restore CA2222 // Do not decrease inherited member visibility
        {
            this.InitializationWaiter = new ManualResetEvent(false);
        }

        public K8sEnvironment Create(TimeSpan timeout, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory?.CreateLogger<K8sEnvironment>();
            this.settings = new KubeHttpClientSettingsProvider(loggerFactory);
            this.httpClient = new KubeHttpClient(settings);
            this.podReadyWatcher = new K8sPodWatcher(loggerFactory);
            this.podReadyWatcher.Changed += K8sPodChanged;
            // Fire & forget on purpose.
            Task.Run(() =>
            {
                podReadyWatcher.StartWatchAsync(httpClient).ConfigureAwait(false);
            });
            Task.Run(() =>
            {
                InitializationWaiter.WaitOne((int)timeout.TotalMilliseconds);
            });
            return this;
        }


        /// <summary>
        /// Async factory method to build the instance of this class.
        /// </summary>
        /// <returns></returns>
        public static async Task<K8sEnvironment> CreateAsync(TimeSpan timeout, ILoggerFactory loggerFactory)
        {
            if (Current == null)
            {
                Current = new K8sEnvironment();
                Current.Create(timeout, loggerFactory);
            }
            await Task.Run(() =>
            {
                Current.InitializationWaiter.WaitOne();
            }).ConfigureAwait(false);
            return Current;
        }

        private async void K8sPodChanged(object sender, K8sWatcherEventArgs e)
        {
            try
            {
                using (K8sQueryClient queryClient = new K8sQueryClient(this.httpClient))
                {
                    K8sPod myPod = await queryClient.GetMyPodAsync().ConfigureAwait(false);
                    this.myPod = myPod;
                    logger?.LogDebug(Invariant($"Getting container status of container-id: {settings.ContainerId}"));
                    this.myContainerStatus = myPod.GetContainerStatus(settings.ContainerId);

                    IEnumerable<K8sReplicaSet> replicaSetList = await queryClient.GetReplicasAsync().ConfigureAwait(false);
                    this.myReplicaSet = myPod.GetMyReplicaSet(replicaSetList);

                    if (this.myReplicaSet != null)
                    {
                        IEnumerable<K8sDeployment> deploymentList = await queryClient.GetDeploymentsAsync().ConfigureAwait(false);
                        this.myDeployment = this.myReplicaSet.GetMyDeployment(deploymentList);
                    }

                    if (this.myPod != null)
                    {
                        IEnumerable<K8sNode> nodeList = await queryClient.GetNodesAsync().ConfigureAwait(false);
                        string nodeName = this.myPod.Spec.NodeName;
                        if (!string.IsNullOrEmpty(nodeName))
                        {
                            this.myNode = nodeList.FirstOrDefault(node => !string.IsNullOrEmpty(node.Metadata?.Name) && node.Metadata.Name.Equals(nodeName, StringComparison.OrdinalIgnoreCase));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogCritical(ex.ToString());
                InitializationWaiter.Reset();
            }
            finally
            {
                // Signal that initialization is done.
                InitializationWaiter.Set();
            }
        }

        /// <summary>
        /// Wait until the container is ready.
        /// Refer document @ https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle/#pod-phase for the Pod's lifecycle.
        /// </summary>
        /// <param name="timeout">Timeout on Application Insights data when the container is not ready after the period.</param>
        /// <param name="client">Query client to try getting info from the Kubernetes cluster API.</param>
        /// <param name="myContainerId">The container that we are interested in.</param>
        /// <returns></returns>
        private static async Task<bool> SpinWaitContainerReady(TimeSpan timeout, K8sQueryClient client, string myContainerId, ILogger<K8sEnvironment> logger)
        {
            DateTime tiemoutAt = DateTime.Now.Add(timeout);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            K8sPod myPod = null;
            do
            {
                // When my pod become available and it's status become ready, we recognize the container is ready.
                myPod = await client.GetMyPodAsync().ConfigureAwait(false);
                if (myPod != null && myPod.GetContainerStatus(myContainerId).Ready)
                {
                    stopwatch.Stop();
                    logger?.LogDebug(Invariant($"K8s info avaialbe in: {stopwatch.ElapsedMilliseconds} ms."));
                    return true;
                }

                // The time to get the container ready dependes on how much time will a container to be initialized.
                // But the minimum seems about 1000ms. Try invoke a probe on readiness every 500ms until the container is ready
                // Or it will timeout per the timeout settings.
                await Task.Delay(500).ConfigureAwait(false);
            } while (DateTime.Now < tiemoutAt);
            return false;
        }

        #region Shorthands to the properties
        /// <summary>
        /// ContainerID for the current K8s entity.
        /// </summary>
        public string ContainerID => this.settings?.ContainerId;

        /// <summary>
        /// Name of the container specificed in deployment spec.
        /// </summary>
        public string ContainerName => this.myContainerStatus?.Name;

        /// <summary>
        /// Name of the Pod
        /// </summary>
        public string PodName => this.myPod?.Metadata?.Name;

        /// <summary>
        /// GUID for a Pod
        /// </summary>
        public string PodID => this.myPod?.Metadata?.Uid;

        /// <summary>
        /// Labels for a pod
        /// </summary>
        public string PodLabels
        {
            get
            {
                string result = null;
                IDictionary<string, string> labelDict = myPod?.Metadata?.Labels;
                if (labelDict != null && labelDict.Count > 0)
                {
                    result = JoinKeyValuePairs(labelDict);
                }
                return result;
            }
        }

        public string ReplicaSetUid => this.myReplicaSet?.Metadata?.Uid;
        public string ReplicaSetName => this.myReplicaSet?.Metadata?.Name;

        public string DeploymentUid => this.myDeployment?.Metadata?.Uid;
        public string DeploymentName => this.myDeployment?.Metadata?.Name;

        public string NodeName => this.myNode?.Metadata?.Name;
        public string NodeUid => this.myNode?.Metadata?.Uid;
        #endregion

        private string JoinKeyValuePairs(IDictionary<string, string> dictionary)
        {
            return string.Join(",", dictionary.Select(kvp => kvp.Key + ':' + kvp.Value));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (httpClient != null)
                    {
                        httpClient.Dispose();
                        httpClient = null;
                    }
                    if (podReadyWatcher != null)
                    {
                        podReadyWatcher.Dispose();
                        podReadyWatcher.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~K8sEnvironment() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
