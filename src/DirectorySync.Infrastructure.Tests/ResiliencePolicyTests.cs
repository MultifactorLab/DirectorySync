using System.Net;
using System.Text;
using DirectorySync.Infrastructure.Configurations;
using DirectorySync.Infrastructure.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Testing;

namespace DirectorySync.Infrastructure.Tests;

public class ResiliencePolicyTests
{
    [Fact]
    public void GetDefaultRetryPolicy_ShouldHaveExpectedOptions()
    {
        // Arrange
        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(ResiliencePolicy.GetDefaultRetryPolicy())
            .Build();

        // Act
        var descriptor = pipeline.GetPipelineDescriptor();

        // Assert
        var retryOptions = Assert.IsType<HttpRetryStrategyOptions> (descriptor.Strategies[0].Options);

        Assert.Equal(2, retryOptions.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), retryOptions.Delay);
        Assert.Equal(DelayBackoffType.Exponential, retryOptions.BackoffType);
    }

    [Fact]
    public async Task GetDefaultRetryPolicy_ShouldHaveExpectedCallCount()
    {
        // Arrange
        int callCount = 0;

        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(ResiliencePolicy.GetDefaultRetryPolicy())
            .Build();

        // Act
        await pipeline.ExecuteAsync(async _ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        var descriptor = pipeline.GetPipelineDescriptor();

        // Assert
        var retryOptions = Assert.IsType<HttpRetryStrategyOptions>(descriptor.Strategies[0].Options);
        Assert.Equal(retryOptions.MaxRetryAttempts + 1, callCount); // initial try + retries
    }

    [Fact]
    public async Task ForbiddenPolicy_ShouldThrowForbiddenException_WhenStatusCodeIs403()
    {
        // Arrange
        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddFallback(ResiliencePolicy.GetForbiddenPolicy())
            .Build();

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await pipeline.ExecuteAsync(async _ =>
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("test error", Encoding.UTF8, "text/plain")
                };
            })
        );
    }

    [Fact]
    public async Task UnauthorizedPolicy_ShouldThrowUnauthorizedException_WhenStatusCodeIs401()
    {
        // Arrange
        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddFallback(ResiliencePolicy.GetUnauthorizedPolicy())
            .Build();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await pipeline.ExecuteAsync(async _ =>
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("test error", Encoding.UTF8, "text/plain")
                };
            })
        );
    }

    [Fact]
    public async Task ConflictPolicy_ShouldThrowConflictException_WhenSyncDisabled()
    {
        // Arrange
        var services = new ServiceCollection();

        var serviceProvider = services.BuildServiceProvider();

        var fallbackOptions = ResiliencePolicy.GetConflictPolicy(serviceProvider);

        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddFallback(fallbackOptions)
            .Build();

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(async () =>
            await pipeline.ExecuteAsync(async _ =>
            {
                return new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent("test error", Encoding.UTF8, "text/plain")
                };
            })
        );
    }
}
