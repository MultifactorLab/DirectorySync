using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Integrations.Multifactor.Updating;
using DirectorySync.Infrastructure.Http;
using DirectorySync.Infrastructure.Integrations.Multifactor.Dto;
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
            .Select(x => new NewUserDto(x.Identity, x.Properties.Select(p => new UserPropertyDto(p.Name, p.Value))));
        var dto = new CreateUsersDto(dtos);

        var cli = _clientFactory.CreateClient(_clientName);
        var adapter = new HttpClientAdapter(cli);

        var response = await adapter.PostAsync<CreateUsersResponseDto>("ds/users", dto);
        var result = new CreateUsersOperationResult();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Recieved unsuccessfull response from Multifactor API");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Recieved 401 status from Multifactor API. Check API integration");
            }

            return result;
        }

        if (response.Model is null)
        {
            _logger.LogWarning("Response model is null");
            return result;
        }

        if (response.Model.Failures.Length == 0)
        {
            result.Add(bucket.NewUsers.Select(x => x.Identity));
        }
        else
        {
            var failures = response.Model.Failures
                .Where(x => !string.IsNullOrWhiteSpace(x?.Identity))
                .Select(x => x.Identity!);

            result.Add(bucket.NewUsers.Select(x => x.Identity).Except(failures));
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

        throw new NotImplementedException();
    }

    public async Task<IUpdateUsersOperationResult> UpdateManyAsync(IModifiedUsersBucket bucket, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bucket);

        if (bucket.ModifiedUsers.Count == 0)
        {
            return new UpdateUsersOperationResult();
        }

        throw new NotImplementedException();
    }
}
