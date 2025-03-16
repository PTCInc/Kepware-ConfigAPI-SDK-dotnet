using Kepware.SyncService.Configuration;
using Kepware.Api.Model;
using Kepware.SyncService.ProjectStorage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Kepware.Api;
using System.Runtime.CompilerServices;
using Kepware.SyncService.Configuration.RuntimeOverwrite;

namespace Kepware.SyncService
{
    public class SyncService : BackgroundService, IDisposable
    {
        private readonly ILogger<SyncService> m_logger;
        private readonly IProjectStorage m_projectStorage;
        private readonly KepwareApiClient m_kepServerClient;
        private readonly Dictionary<KepwareApiClient, long?> m_secondaryClients;
        private readonly KepSyncOptions m_syncOptions;

        private readonly ConcurrentQueue<ChangeEvent> m_changeQueue = new ConcurrentQueue<ChangeEvent>();

        private IDisposable? m_storageChangeEvents = null;
        private bool m_bIsDisposed = false;
        private long? m_lastProjectId = null;

        public SyncService(KepwareApiClient primaryClient, List<KepwareApiClient> secondaryClients, IProjectStorage projectStorage, KepSyncOptions syncOptions, ILogger<SyncService> logger)
        {
            m_logger = logger;
            m_syncOptions = syncOptions;
            m_kepServerClient = primaryClient;
            m_secondaryClients = secondaryClients.ToDictionary(client => client, client => null as long?);
            m_projectStorage = projectStorage;
        }

        private Task InititalizeAsync()
        {
            m_storageChangeEvents = m_projectStorage.ObserveChanges()
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(changeEvent => NotifyChange(new ChangeEvent { Source = ChangeSource.LocalFile, Reason = "Files " + changeEvent.Type.ToString() }));

            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            m_logger.LogInformation("Starting SyncService...");

            await InititalizeAsync();

            if (m_syncOptions.SyncDirection == SyncDirection.DiskToKepware)
            {
                NotifyChange(new ChangeEvent { Source = ChangeSource.LocalFile, Reason = "Initial sync from local file" });
            }
            else
            {
                NotifyChange(new ChangeEvent { Source = ChangeSource.PrimaryKepServer, Reason = "Initial sync from Kepware" });
            }
            bool blnFirstDisconnect = true;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await m_kepServerClient.TestConnectionAsync(stoppingToken))
                    {
                        if (m_syncOptions.SyncMode == SyncMode.TwoWay ||
                            m_syncOptions.SyncDirection == SyncDirection.KepwareToDisk)
                        {
                            if (m_changeQueue.TryDequeue(out var changeEvent))
                            {
                                while (m_changeQueue.TryPeek(out var nextPending) && nextPending.Source == changeEvent!.Source)
                                {
                                    m_changeQueue.TryDequeue(out changeEvent);
                                }
                                await ProcessChangeAsync(changeEvent!, stoppingToken);
                            }
                            else
                            {
                                var currentProjectId = await FetchCurrentProjectIdAsync(m_kepServerClient, stoppingToken);
                                if (m_lastProjectId != currentProjectId)
                                {
                                    await ProcessChangeAsync(new ChangeEvent { Source = ChangeSource.PrimaryKepServer, Reason = "Project changed" }, stoppingToken);
                                }
                            }
                        }

                        blnFirstDisconnect = true;
                    }
                    else
                    {
                        // no connection to kepserver - wait and try again
                        if (blnFirstDisconnect)
                        {
                            m_logger.LogWarning("No connection to {ClientName}-client. Waiting for connection...", m_kepServerClient.ClientName);
                            blnFirstDisconnect = false;
                        }
                        await Task.Delay(5000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error while processing change event");
                }
                await Task.Delay(Math.Max(500, m_syncOptions.SyncThrottlingMs), stoppingToken);
            }
        }

        private static async Task<long> FetchCurrentProjectIdAsync(KepwareApiClient client, CancellationToken cancellationToken)
        {
            var project = await client.Project.LoadProject(false, cancellationToken).ConfigureAwait(false);

            return project?.ProjectId ?? -1;
        }

        public void NotifyChange(ChangeEvent changeEvent)
        {
            var target = changeEvent.Source == ChangeSource.PrimaryKepServer ? ChangeSource.LocalFile : ChangeSource.PrimaryKepServer;
            m_logger.LogInformation("Enqueued sync request: {ChangeSource} -> {Target} ({Change})", changeEvent.Source, target, changeEvent.Reason);
            m_changeQueue.Enqueue(changeEvent);
        }

        private async Task ProcessChangeAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
        {
            m_logger.LogInformation("Processing Sync (most resent event: {ChangeSource} - {Change})", changeEvent.Source, changeEvent.Reason);
            try
            {
                switch (changeEvent.Source)
                {
                    case ChangeSource.PrimaryKepServer:
                        await SyncFromPrimaryKepServerAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case ChangeSource.LocalFile:
                        await SyncFromLocalFileAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case ChangeSource.SecondaryKepServer:
                        await SyncFromSecondaryKepServerAsync(cancellationToken).ConfigureAwait(false);
                        break;
                }

                m_lastProjectId = await FetchCurrentProjectIdAsync(m_kepServerClient, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error while processing change event");
            }
        }


        internal async Task SyncFromPrimaryKepServerAsync(CancellationToken cancellationToken = default)
        {
            m_logger.LogInformation("Synchronizing full project from primary Kepware...");
            var project = await m_kepServerClient.Project.LoadProject(true);
            await project.Cleanup(m_kepServerClient, true, cancellationToken);

            if (m_kepServerClient.ClientOptions.Tag is KepwareSyncTarget targetOptions &&
               !string.IsNullOrEmpty(targetOptions.OverwriteConfigFile) &&
               File.Exists(targetOptions.OverwriteConfigFile))
            {
                var overwriteConfig = await RuntimeOverwriteConfig.LoadFromYamlFileAsync(targetOptions.OverwriteConfigFile, cancellationToken);

                if (overwriteConfig.Apply(project))
                {
                    m_logger.LogInformation("Applied overwrite config from {OverwriteFile} to project before synchronisation from primary kepserver", targetOptions.OverwriteConfigFile);
                    await SyncProjectToKepServerAsync("primary-overwrite-file", project, m_kepServerClient, m_kepServerClient.ClientName, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }


            await m_projectStorage.ExportProjecAsync(project, cancellationToken).ConfigureAwait(false);

            m_lastProjectId = project.ProjectId;


            int i = 0;
            foreach (var secondaryClient in m_secondaryClients.Keys)
            {
                try
                {
                    if (await secondaryClient.TestConnectionAsync(cancellationToken).ConfigureAwait(false))
                    {
                        await SyncProjectToKepServerAsync("primary", project, secondaryClient, $"Secondary-{++i:00}", cancellationToken: cancellationToken).ConfigureAwait(false);

                        var currentProjectId = await FetchCurrentProjectIdAsync(secondaryClient, cancellationToken).ConfigureAwait(false);
                        m_secondaryClients[secondaryClient] = currentProjectId;
                    }
                    else
                    {
                        m_secondaryClients[secondaryClient] = null;
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error while syncing project to secondary client {ClientName}", secondaryClient.ClientHostName);
                }
            }
        }

        private async Task SyncFromSecondaryKepServerAsync(CancellationToken cancellationToken)
        {
            m_logger.LogInformation("Synchronizing full project from secondary Kepware...");

            //find the clients that have changed
            List<KepwareApiClient> changedClients = new List<KepwareApiClient>();
            foreach (var kvp in m_secondaryClients)
            {
                if (kvp.Value != null && await FetchCurrentProjectIdAsync(kvp.Key, cancellationToken) != kvp.Value)
                {
                    changedClients.Add(kvp.Key);
                }
            }

            if (changedClients.Count >= 1)
            {
                var clientToSyncFrom = changedClients[0];
                if (changedClients.Count > 1)
                {
                    m_logger.LogWarning("Multiple secondary clients have changed. Syncing from the first one ({ClientHostName}).",
                       clientToSyncFrom.ClientHostName);
                }
                else
                {
                    m_logger.LogInformation("Syncing from secondary client {ClientHostName}...", clientToSyncFrom.ClientHostName);
                }

                var projectFromSecondary = await clientToSyncFrom.Project.LoadProject(true, cancellationToken).ConfigureAwait(false);
                await SyncProjectToKepServerAsync("secondary", projectFromSecondary, m_kepServerClient, "Primary",
                    onSyncedWithChanges: () => NotifyChange(new ChangeEvent { Source = ChangeSource.PrimaryKepServer, Reason = "Sync from secondary kepserver" }),
                    cancellationToken: cancellationToken).ConfigureAwait(false);


            }
            else
            {
                m_logger.LogInformation("No changes in secondary clients");
            }
        }


        internal async Task SyncFromLocalFileAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                m_logger.LogInformation("Synchronizing full project from local file...");
                var projectFromDisk = await m_projectStorage.LoadProject(true, cancellationToken).ConfigureAwait(false);
                await SyncProjectToKepServerAsync("disk", projectFromDisk, m_kepServerClient, "Primary",
                    onSyncedWithChanges: () => NotifyChange(new ChangeEvent { Source = ChangeSource.PrimaryKepServer, Reason = "Sync from kepserver after filesync" }),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error while syncing local project file to Kepware");
            }
        }

        private async Task SyncProjectToKepServerAsync(string projectSource, Project project, KepwareApiClient kepServerClient, string clientName, Action? onSyncedWithChanges = default, CancellationToken cancellationToken = default)
        {
            if (kepServerClient.ClientOptions.Tag is KepwareSyncTarget targetOptions &&
                !string.IsNullOrEmpty(targetOptions.OverwriteConfigFile) &&
                File.Exists(targetOptions.OverwriteConfigFile))
            {
                m_logger.LogInformation("Applying overwrite config from {OverwriteFile} to project before synchronisation from {ProjectSource} to {ClientName}-kepserver ({ClientHostName})",
                    targetOptions.OverwriteConfigFile, projectSource, clientName, kepServerClient.ClientHostName);

                project = await project.CloneAsync(cancellationToken).ConfigureAwait(false);
                var overwrite= await RuntimeOverwriteConfig.LoadFromYamlFileAsync(targetOptions.OverwriteConfigFile, cancellationToken).ConfigureAwait(false);
                overwrite.Apply(project);
            }

            var (inserts, updates, deletes) = await kepServerClient.Project.CompareAndApply(project, cancellationToken).ConfigureAwait(false);

            if (updates > 0 || deletes > 0 || inserts > 0)
            {
                m_logger.LogInformation("Completed synchronisation from {ProjectSource} to {ClientName}-kepserver ({ClientHostName}): {NumUpdates} updates, {NumInserts} inserts, {NumDeletes} deletes",
                 projectSource, clientName, kepServerClient.ClientHostName, updates, inserts, deletes);
                onSyncedWithChanges?.Invoke();
            }
            else
            {
                m_logger.LogInformation("Completed synchronisation from {ProjectSource} to {ClientName}-kepserver ({ClientHostName}):: no changes made",
                    projectSource, clientName, kepServerClient.ClientHostName);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_bIsDisposed)
            {
                if (disposing)
                {
                    m_storageChangeEvents?.Dispose();
                    m_storageChangeEvents = null;
                }

                m_bIsDisposed = true;
            }
        }



        public override void Dispose()
        {
            base.Dispose();
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

