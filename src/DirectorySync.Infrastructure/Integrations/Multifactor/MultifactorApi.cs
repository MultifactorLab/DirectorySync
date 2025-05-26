using System.Text.Json;
using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Integrations.Multifactor.Updating;
using DirectorySync.Infrastructure.Common.Dto;
using DirectorySync.Infrastructure.Integrations.Multifactor.Dto;
using DirectorySync.Infrastructure.Integrations.Multifactor.Dto.Create;
using DirectorySync.Infrastructure.Integrations.Multifactor.Dto.Delete;
using DirectorySync.Infrastructure.Integrations.Multifactor.Dto.Update;
using DirectorySync.Infrastructure.Shared.Http;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Infrastructure.Integrations.Multifactor;

internal class MultifactorApi : IMultifactorApi
{
    private const string _clientName = "MultifactorApi";

    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<MultifactorApi> _logger;

    public MultifactorApi(IHttpClientFactory clientFactory, 
        ILogger<MultifactorApi> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<ICreateUsersOperationResult> CreateManyAsync(INewUsersBucket bucket, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bucket);

        if (bucket.NewUsers.Count == 0)
        {
            return new CreateUsersOperationResult();
        }

        var dtos = bucket.NewUsers
            .Select(x => new NewUserDto(x.Identity, 
            x.Properties.Select(p => new UserPropertyDto(p.Name, p.Value)),
            new SignUpGroupChangesDto(x.SignUpGroupChanges.SignUpGroupsToAdd, x.SignUpGroupChanges.SignUpGroupsToRemove)));
        var dto = new CreateUsersDto(dtos);

        var cli = _clientFactory.CreateClient(_clientName);
        var adapter = new HttpClientAdapter(cli);
        var response = await adapter.PostAsync<CreateUsersResponseDto>("v2/ds/users", dto);
        var result = new CreateUsersOperationResult();

        if (!response.IsSuccessStatusCode)
        {
            LogUnseccessfulResponse(response);

            return result;
        }

        if (response.Model is null)
        {
            _logger.LogWarning("Response model is null");
            return result;
        }

        if (response.Model.Failures.Length == 0)
        {
            result.Add(bucket.NewUsers.Select(x => new HandledUser(x.Id, x.Identity)));
        }
        else
        {
            var failures = response.Model.Failures
                .Where(x => !string.IsNullOrWhiteSpace(x?.Identity))
                .Select(x => x.Identity!);

            var success = bucket.NewUsers
                .ExceptBy(failures, newUser => newUser.Identity);

            result.Add(success.Select(x => new HandledUser(x.Id, x.Identity)));
        }

        return result;
    }

    public async Task<IUpdateUsersOperationResult> UpdateManyAsync(IModifiedUsersBucket bucket, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bucket);

        if (bucket.ModifiedUsers.Count == 0)
        {
            return new UpdateUsersOperationResult();
        }

        var dtos = bucket.ModifiedUsers
            .Select(x => new ModifiedUserDto(
                x.Identity,
                x.Properties.Select(s => new UserPropertyDto(s.Name, s.Value)),
                new SignUpGroupChangesDto(x.SignUpGroupChanges.SignUpGroupsToAdd, x.SignUpGroupChanges.SignUpGroupsToRemove)));
        var dto = new UpdateUsersDto(dtos);

        var cli = _clientFactory.CreateClient(_clientName);
        var adapter = new HttpClientAdapter(cli);

        var response = await adapter.PutAsync<UpdateUsersResponseDto>("v2/ds/users", dto);
        var result = new UpdateUsersOperationResult();
        if (!response.IsSuccessStatusCode)
        {
            LogUnseccessfulResponse(response);

            return result;
        }

        if (response.Model is null)
        {
            _logger.LogWarning("Response model is null");
            return result;
        }

        if (response.Model.Failures.Length == 0)
        {
            result.Add(bucket.ModifiedUsers.Select(x => new HandledUser(x.Id, x.Identity)));
        }
        else
        {
            var failures = response.Model.Failures
                .Where(x => !string.IsNullOrWhiteSpace(x?.Identity))
                .Select(x => x.Identity!);

            var success = bucket.ModifiedUsers
                .ExceptBy(failures, newUser => newUser.Identity);

            result.Add(success.Select(x => new HandledUser(x.Id, x.Identity)));
        }

        return result;
    }

    public async Task<IDeleteUsersOperationResult> DeleteManyAsync(IDeletedUsersBucket bucket, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bucket);

        if (bucket.DeletedUsers.Count == 0)
        {
            return new DeleteUsersOperationResult();
        }

        var dto = new DeleteUsersDto(bucket.DeletedUsers.Select(x => x.Identity));

        var cli = _clientFactory.CreateClient(_clientName);
        var adapter = new HttpClientAdapter(cli);

        var response = await adapter.DeleteAsync<DeleteUsersResponseDto>("ds/users", dto);
        var result = new DeleteUsersOperationResult();
        if (!response.IsSuccessStatusCode)
        {
            LogUnseccessfulResponse(response);

            return result;
        }

        if (response.Model is null)
        {
            _logger.LogWarning("Response model is null");
            return result;
        }

        if (response.Model.Failures.Length == 0)
        {
            result.Add(bucket.DeletedUsers.Select(x => new HandledUser(x.Id, x.Identity)));
        }
        else
        {
            var failures = response.Model.Failures
                .Where(x => !string.IsNullOrWhiteSpace(x?.Identity))
                .Select(x => x.Identity!);

            var success = bucket.DeletedUsers
                .ExceptBy(failures, newUser => newUser.Identity);

            result.Add(success.Select(x => new HandledUser(x.Id, x.Identity)));
        }

        return result;
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
