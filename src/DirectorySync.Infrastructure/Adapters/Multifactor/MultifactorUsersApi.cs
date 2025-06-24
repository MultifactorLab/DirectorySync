using System.Collections.ObjectModel;
using System.Text.Json;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Infrastructure.Common.Dto;
using DirectorySync.Infrastructure.Dto.Multifactor.Users.Create;
using DirectorySync.Infrastructure.Dto.Multifactor.Users.Delete;
using DirectorySync.Infrastructure.Dto.Multifactor.Users.Get;
using DirectorySync.Infrastructure.Dto.Multifactor.Users.Update;
using DirectorySync.Infrastructure.Shared.Http;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Infrastructure.Adapters.Multifactor;

public class MultifactorUsersApi : IUserCloudPort
{
    private const string _clientName = "MultifactorApi";

    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<MultifactorUsersApi> _logger;

    public MultifactorUsersApi(IHttpClientFactory clientFactory, 
        ILogger<MultifactorUsersApi> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }
    
    public async Task<ReadOnlyCollection<Identity>> GetUsersIdentitesAsync(CancellationToken ct = default)
    {
        var client = _clientFactory.CreateClient(_clientName);
        var adapter = new HttpClientAdapter(client);
        var response = await adapter.GetAsync<GetIdentitiesResponse>("ds/users");

        if (!response.IsSuccessStatusCode)
        {
            LogUnseccessfulResponse(response);
            return ReadOnlyCollection<Identity>.Empty;
        }
        
        var identites = GetIdentitiesResponse.ToDomainModels(response.Model);
        
        return identites.ToArray().AsReadOnly();
    }

    public async Task<ReadOnlyCollection<MemberModel>> CreateManyAsync(IEnumerable<MemberModel> newMembers, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(newMembers);

        var newUsers = newMembers.ToList();

        if (newUsers.Count == 0)
        {
            return ReadOnlyCollection<MemberModel>.Empty;
        }
            
        var dto = CreateUsersRequest.FromDomainModels(newUsers);
        
        var client = _clientFactory.CreateClient(_clientName);
        var adapter = new HttpClientAdapter(client);
        var response = await adapter.PostAsync<CreateUsersResponse>("v2/ds/users", dto);
    
        if (!response.IsSuccessStatusCode)
        {
            LogUnseccessfulResponse(response);
            return ReadOnlyCollection<MemberModel>.Empty;
        }
        
        if (response.Model is null)
        {
            _logger.LogWarning("Response model is null");
            return ReadOnlyCollection<MemberModel>.Empty;
        }

        if (response.Model.Failures.Length != 0)
        {
            var failures = response.Model.Failures
                .Where(x => !string.IsNullOrWhiteSpace(x?.Identity))
                .Select(x => x.Identity!);
            
            newUsers.RemoveAll(c => failures.Contains(c.Identity));
        }
        
        return newUsers.AsReadOnly();
    }

    public async Task<ReadOnlyCollection<MemberModel>> UpdateManyAsync(IEnumerable<MemberModel> updMembers, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(updMembers);

        var updUsers = updMembers.ToList();

        if (updUsers.Count == 0)
        {
            return ReadOnlyCollection<MemberModel>.Empty;
        }
            
        var dto = UpdateUsersRequest.FromDomainModels(updUsers);
        
        var client = _clientFactory.CreateClient(_clientName);
        var adapter = new HttpClientAdapter(client);
        var response = await adapter.PutAsync<UpdateUsersResponse>("v2/ds/users", dto);
    
        if (!response.IsSuccessStatusCode)
        {
            LogUnseccessfulResponse(response);
            return ReadOnlyCollection<MemberModel>.Empty;
        }
        
        if (response.Model is null)
        {
            _logger.LogWarning("Response model is null");
            return ReadOnlyCollection<MemberModel>.Empty;
        }

        if (response.Model.Failures.Length != 0)
        {
            var failures = response.Model.Failures
                .Where(x => !string.IsNullOrWhiteSpace(x?.Identity))
                .Select(x => x.Identity!);
            
            updUsers.RemoveAll(c => failures.Contains(c.Identity));
        }
        
        return updUsers.AsReadOnly();
    }

    public async Task<ReadOnlyCollection<MemberModel>> DeleteManyAsync(IEnumerable<MemberModel> delMembers, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(delMembers);

        var delUsers = delMembers.ToList();

        if (delUsers.Count == 0)
        {
            return ReadOnlyCollection<MemberModel>.Empty;
        }
            
        var dto = DeleteUsersRequest.FromDomainModels(delUsers);
        
        var client = _clientFactory.CreateClient(_clientName);
        var adapter = new HttpClientAdapter(client);
        var response = await adapter.DeleteAsync<DeleteUsersResponse>("v2/ds/users", dto);
    
        if (!response.IsSuccessStatusCode)
        {
            LogUnseccessfulResponse(response);
            return ReadOnlyCollection<MemberModel>.Empty;
        }
        
        if (response.Model is null)
        {
            _logger.LogWarning("Response model is null");
            return ReadOnlyCollection<MemberModel>.Empty;
        }

        if (response.Model.Failures.Length != 0)
        {
            var failures = response.Model.Failures
                .Where(x => !string.IsNullOrWhiteSpace(x?.Identity))
                .Select(x => x.Identity!);
            
            delUsers.RemoveAll(c => failures.Contains(c.Identity));
        }
        
        return delUsers.AsReadOnly();
    }
    
    private void LogUnseccessfulResponse(HttpClientResponse response)
    {
        var options = new JsonSerializerOptions()
        
        {
            PropertyNameCaseInsensitive = true,
        };
        var errorResponse = JsonSerializer.Deserialize<UnsuccessfulResponse>(response.Content, options);
        var statusCode = Convert.ToInt32(response.StatusCode);

        _logger.LogWarning("Multifactor API request failed with status {0}. Error message: {1}",
            statusCode,
            errorResponse?.Message);
    }
}
