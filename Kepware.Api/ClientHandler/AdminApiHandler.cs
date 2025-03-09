using Kepware.Api.Model;
using Kepware.Api.Model.Admin;
using Kepware.Api.Serializer;
using Kepware.Api.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Kepware.Api.ClientHandler
{
    public class AdminApiHandler
    {
        private readonly KepwareApiClient m_kepwareApiClient;
        private readonly ILogger<AdminApiHandler> m_logger;

        public AdminApiHandler(KepwareApiClient kepwareApiClient, ILogger<AdminApiHandler> logger)
        {
            m_kepwareApiClient = kepwareApiClient;
            m_logger = logger;
        }

        #region AdminSettings

        /// <summary>
        /// Retrieves the current administrator settings asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The current <see cref="AdminSettings"/> or null if retrieval fails.</returns>
        public Task<AdminSettings?> GetAdminSettingsAsync(CancellationToken cancellationToken = default)
        {
            return m_kepwareApiClient.GenericConfig.LoadEntityAsync<AdminSettings>(name: null, cancellationToken);
        }

        /// <summary>
        /// Updates the administrator settings with the specified values.
        /// </summary>
        /// <param name="settings">The new administrator settings to apply.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>True if the settings were successfully updated; otherwise, false.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the current settings cannot be retrieved.</exception>
        public async Task<bool> SetAdminSettingsAsync(AdminSettings settings, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentSettings = await GetAdminSettingsAsync(cancellationToken).ConfigureAwait(false);

                if (currentSettings == null)
                {
                    throw new InvalidOperationException("Failed to retrieve current settings");
                }

                var endpoint = EndpointResolver.ResolveEndpoint<AdminSettings>();
                var diff = settings.GetUpdateDiff(currentSettings);

                m_logger.LogInformation("Updating AdminSettings on {Endpoint}, values {Diff}", endpoint, diff);

                HttpContent httpContent = new StringContent(
                    JsonSerializer.Serialize(diff, KepJsonContext.Default.DictionaryStringJsonElement),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await m_kepwareApiClient.HttpClient.PutAsync(endpoint, httpContent, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    m_logger.LogError("Failed to update AdminSettings from {Endpoint}: {ReasonPhrase}\n{Message}", endpoint, response.ReasonPhrase, message);
                }
                else
                {
                    return true;
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_kepwareApiClient.HttpClient.BaseAddress);
                m_kepwareApiClient.OnHttpRequestException(httpEx);
            }

            return false;
        }

        #endregion

        #region UaEndpoint

        /// <summary>
        /// Retrieves a collection of OPC UA endpoints asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>A collection of <see cref="UaEndpoint"/> or null if retrieval fails.</returns>
        public Task<UaEndpointCollection?> GetUaEndpointListAsync(CancellationToken cancellationToken = default)
        {
            return m_kepwareApiClient.GenericConfig.LoadCollectionAsync<UaEndpointCollection, UaEndpoint>(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Retrieves an OPC UA endpoint configuration asynchronously.
        /// </summary>
        /// <param name="name">The name of the OPC UA endpoint.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The <see cref="UaEndpoint"/> configuration, or null if not found.</returns>
        public Task<UaEndpoint?> GetUaEndpointAsync(string name, CancellationToken cancellationToken = default)
        {
            return m_kepwareApiClient.GenericConfig.LoadEntityAsync<UaEndpoint>(name, cancellationToken);
        }

        /// <summary>
        /// Creates a new OPC UA endpoint or updates an existing one.
        /// </summary>
        /// <param name="endpoint">The <see cref="UaEndpoint"/> to create or update.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="endpoint"/> has no name specified.</exception>
        public async Task<bool> CreateOrUpdateUaEndpointAsync(UaEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(endpoint.Name))
                throw new ArgumentException("Name is required", nameof(endpoint));

            try
            {
                var endpointUrl = EndpointResolver.ResolveEndpoint<UaEndpoint>([endpoint.Name]);
                var currentEndpoint = await m_kepwareApiClient.GenericConfig.LoadEntityByEndpointAsync<UaEndpoint>(endpointUrl, cancellationToken);

                if (currentEndpoint == null)
                {
                    return await m_kepwareApiClient.GenericConfig.InsertItemAsync(endpoint, cancellationToken: cancellationToken);
                }
                else
                {
                    return await m_kepwareApiClient.GenericConfig.UpdateItemAsync(endpoint: endpointUrl, item: endpoint, currentEntity: currentEndpoint, cancellationToken: cancellationToken);
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_kepwareApiClient.HttpClient.BaseAddress);
                m_kepwareApiClient.OnHttpRequestException(httpEx);
            }

            return false;
        }

        /// <summary>
        /// Deletes an OPC UA endpoint configuration asynchronously.
        /// </summary>
        /// <param name="name">The name of the endpoint to delete.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>True if the endpoint was successfully deleted; otherwise, false.</returns>
        public Task<bool> DeleteUaEndpointAsync(string name, CancellationToken cancellationToken = default)
            => m_kepwareApiClient.GenericConfig.DeleteItemAsync<UaEndpoint>(name, cancellationToken);

        #endregion

        #region ServerUserGroup

        /// <summary>
        /// Retrieves a collection of Server User Groups asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>A collection of <see cref="ServerUserGroup"/> or null if retrieval fails.</returns>
        public Task<ServerUserGroupCollection?> GetServerUserGroupListAsync(CancellationToken cancellationToken = default)
        {
            return m_kepwareApiClient.GenericConfig.LoadCollectionAsync<ServerUserGroupCollection, ServerUserGroup>(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Retrieves a Server User Group configuration asynchronously.
        /// </summary>
        /// <param name="name">The name of the Server User Group.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The <see cref="ServerUserGroup"/> configuration, or null if not found.</returns>
        public Task<ServerUserGroup?> GetServerUserGroupAsync(string name, CancellationToken cancellationToken = default)
        {
            return m_kepwareApiClient.GenericConfig.LoadEntityAsync<ServerUserGroup>(name, cancellationToken);
        }

        /// <summary>
        /// Creates a new Server User Group or updates an existing one.
        /// </summary>
        /// <param name="userGroup">The <see cref="ServerUserGroup"/> to create or update.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="userGroup"/> has no name specified.</exception>
        public async Task<bool> CreateOrUpdateServerUserGroupAsync(ServerUserGroup userGroup, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userGroup.Name))
                throw new ArgumentException("Name is required", nameof(userGroup));

            try
            {
                var endpointUrl = EndpointResolver.ResolveEndpoint<ServerUserGroup>([userGroup.Name]);
                var currentGroup = await m_kepwareApiClient.GenericConfig.LoadEntityByEndpointAsync<ServerUserGroup>(endpointUrl, cancellationToken);

                if (currentGroup == null)
                {
                    return await m_kepwareApiClient.GenericConfig.InsertItemAsync(userGroup, cancellationToken: cancellationToken);
                }
                else
                {
                    return await m_kepwareApiClient.GenericConfig.UpdateItemAsync(endpoint: endpointUrl, item: userGroup, currentEntity: currentGroup, cancellationToken: cancellationToken);
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_kepwareApiClient.HttpClient.BaseAddress);
                m_kepwareApiClient.OnHttpRequestException(httpEx);
            }

            return false;
        }

        /// <summary>
        /// Deletes a Server User Group configuration asynchronously.
        /// </summary>
        /// <param name="name">The name of the group to delete.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>True if the group was successfully deleted; otherwise, false.</returns>
        public Task<bool> DeleteServerUserGroupAsync(string name, CancellationToken cancellationToken = default)
            => m_kepwareApiClient.GenericConfig.DeleteItemAsync<ServerUserGroup>(name, cancellationToken);

        #endregion

        #region ServerUser
        /// <summary>
        /// Retrieves a collection of Server Users asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>A collection of <see cref="ServerUser"/> or null if retrieval fails.</returns>
        public Task<ServerUserCollection?> GetServerUserListAsync(CancellationToken cancellationToken = default)
        {
            return m_kepwareApiClient.GenericConfig.LoadCollectionAsync<ServerUserCollection, ServerUser>(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Retrieves a Server User configuration asynchronously.
        /// </summary>
        /// <param name="name">The name of the Server User.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The <see cref="ServerUser"/> configuration, or null if not found.</returns>
        public Task<ServerUser?> GetServerUserAsync(string name, CancellationToken cancellationToken = default)
        {
            return m_kepwareApiClient.GenericConfig.LoadEntityAsync<ServerUser>(name, cancellationToken);
        }

        /// <summary>
        /// Creates a new Server User or updates an existing one.
        /// </summary>
        /// <param name="user">The <see cref="ServerUser"/> to create or update.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="user"/> has no name specified or the password is invalid.</exception>
        public async Task<bool> CreateOrUpdateServerUserAsync(ServerUser user, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(user.Name))
                throw new ArgumentException("Name is required", nameof(user));

            try
            {
                var endpointUrl = EndpointResolver.ResolveEndpoint<ServerUser>([user.Name]);
                var currentUser = await m_kepwareApiClient.GenericConfig.LoadEntityByEndpointAsync<ServerUser>(endpointUrl, cancellationToken);

                if (currentUser == null)
                {
                    if (string.IsNullOrEmpty(user.Password) || user.Password.Length < 14)
#pragma warning disable S3928 // Parameter names used into ArgumentException constructors should match an existing one 
                        throw new ArgumentException("Password must be at least 14 characters long", nameof(user.Password));
#pragma warning restore S3928 // Parameter names used into ArgumentException constructors should match an existing one 

                    return await m_kepwareApiClient.GenericConfig.InsertItemAsync<ServerUserCollection, ServerUser>(user, cancellationToken: cancellationToken);
                }
                else
                {
                    if (!string.IsNullOrEmpty(user.Password) && user.Password.Length < 14)
#pragma warning disable S3928 // Parameter names used into ArgumentException constructors should match an existing one 
                        throw new ArgumentException("Password must be at least 14 characters long", nameof(user.Password));
#pragma warning restore S3928 // Parameter names used into ArgumentException constructors should match an existing one 

                    return await m_kepwareApiClient.GenericConfig.UpdateItemAsync(endpoint: endpointUrl, item: user, currentEntity: currentUser, cancellationToken: cancellationToken);
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_kepwareApiClient.HttpClient.BaseAddress);
                m_kepwareApiClient.OnHttpRequestException(httpEx);
            }

            return false;
        }

        /// <summary>
        /// Deletes a Server User configuration asynchronously.
        /// </summary>
        /// <param name="name">The name of the user to delete.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>True if the user was successfully deleted; otherwise, false.</returns>
        public Task<bool> DeleteServerUserAsync(string name, CancellationToken cancellationToken = default)
            => m_kepwareApiClient.GenericConfig.DeleteItemAsync<ServerUser>(name, cancellationToken);

        #endregion

        #region ProjectPermission

        /// <summary>
        /// Retrieves a collection of project permissions asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>A collection of <see cref="ProjectPermission"/> or null if retrieval fails.</returns>
        public Task<ProjectPermissionCollection?> GetProjectPermissionListAsync(CancellationToken cancellationToken = default)
        {
            return m_kepwareApiClient.GenericConfig.LoadCollectionAsync<ProjectPermissionCollection, ProjectPermission>(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Retrieves a project permission for a specific server user group asynchronously.
        /// </summary>
        /// <param name="serverUserGroup">The server user group for which to retrieve the project permission.</param>
        /// <param name="projectPermissionName">The name of the project permission to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The <see cref="ProjectPermission"/> or null if not found.</returns>
        public Task<ProjectPermission?> GetProjectPermissionAsync(ServerUserGroup serverUserGroup, ProjectPermissionName projectPermissionName, CancellationToken cancellationToken = default)
            => GetProjectPermissionAsync(serverUserGroup.Name, projectPermissionName, cancellationToken);

        /// <summary>
        /// Retrieves a project permission for a specific server user group asynchronously.
        /// </summary>
        /// <param name="serverUserGroupName">The name of the server user group for which to retrieve the project permission.</param>
        /// <param name="projectPermissionName">The name of the project permission to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The <see cref="ProjectPermission"/> or null if not found.</returns>
        public Task<ProjectPermission?> GetProjectPermissionAsync(string serverUserGroupName, ProjectPermissionName projectPermissionName, CancellationToken cancellationToken = default)
        {
            var endpoint = EndpointResolver.ResolveEndpoint<ProjectPermission>([serverUserGroupName, projectPermissionName]);
            return m_kepwareApiClient.GenericConfig.LoadEntityByEndpointAsync<ProjectPermission>(endpoint, cancellationToken);
        }

        /// <summary>
        /// Updates a project permission for a specific server user group asynchronously.
        /// </summary>
        /// <param name="serverUserGroup">The server user group for which to update the project permission.</param>
        /// <param name="projectPermission">The project permission to update.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        public Task<bool> UpdateProjectPermissionAsync(ServerUserGroup serverUserGroup, ProjectPermission projectPermission, CancellationToken cancellationToken = default)
            => UpdateProjectPermissionAsync(serverUserGroup.Name, projectPermission, cancellationToken);

        /// <summary>
        /// Updates a project permission for a specific server user group asynchronously.
        /// </summary>
        /// <param name="serverUserGroupName">The name of the server user group for which to update the project permission.</param>
        /// <param name="projectPermission">The project permission to update.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the project permission cannot be found.</exception>
        public async Task<bool> UpdateProjectPermissionAsync(string serverUserGroupName, ProjectPermission projectPermission, CancellationToken cancellationToken = default)
        {
            try
            {
                var endpointUrl = EndpointResolver.ResolveEndpoint<ProjectPermission>([serverUserGroupName, projectPermission.Name]);
                var existingPermission = await m_kepwareApiClient.GenericConfig.LoadEntityByEndpointAsync<ProjectPermission>(endpointUrl, cancellationToken);

                if (existingPermission == null)
                {
                    throw new InvalidOperationException($"Project permission {projectPermission.Name} not found for {serverUserGroupName}");
                }
                else
                {
                    return await m_kepwareApiClient.GenericConfig.UpdateItemAsync(endpoint: endpointUrl, item: projectPermission, currentEntity: existingPermission, cancellationToken: cancellationToken);
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_kepwareApiClient.HttpClient.BaseAddress);
                m_kepwareApiClient.OnHttpRequestException(httpEx);
            }

            return false;
        }
        #endregion
    }
}
