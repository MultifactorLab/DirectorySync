using DirectorySync.Application.Integrations.Multifactor;
using DirectorySync.Application.Integrations.Multifactor.Creating;
using DirectorySync.Application.Integrations.Multifactor.Deleting;
using DirectorySync.Application.Integrations.Multifactor.Updating;
using DirectorySync.Infrastructure.Http;
using DirectorySync.Infrastructure.Integrations.Multifactor.Dto;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Infrastructure.Integrations.Multifactor;

internal class MultifactorApi(IHttpClientFactory clientFactory, ILogger<MultifactorApi> logger) : IMultifactorApi
{
    private const string _clientName = "MultifactorApi";

    private readonly IHttpClientFactory _clientFactory = clientFactory;
    private readonly ILogger<MultifactorApi> _logger = logger;

    public async Task<ICreateUsersOperationResult> CreateManyAsync(INewUsersBucket bucket, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bucket);

        if (bucket.NewUsers.Count == 0)
        {
            return new CreateUsersOperationResult();
        }

        var dtos = bucket.NewUsers.Select(x => new NewUserDto(x.Identity, x.Properties.Select(p => new UserPropertyDto(p.Name, p.Value))));
        var dto = new CreateUsersDto(dtos);

        var cli = _clientFactory.CreateClient(_clientName);
        var adapter = new HttpClientAdapter(cli);

        var response = await adapter.PostAsync<object>("ds/users", dto);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Got unsuccessfull response from Multifactor API");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Got 401 status from Multifactor API. Maybe invalid API integration");
            }

            return new CreateUsersOperationResult();
        }

        throw new NotImplementedException();
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
