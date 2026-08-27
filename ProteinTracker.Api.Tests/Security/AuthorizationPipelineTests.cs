using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProteinTracker.Api.Data;
using Xunit;

namespace ProteinTracker.Api.Tests.Security;

public class AuthorizationPipelineTests
{
    [Fact(DisplayName = "Application endpoints reject unauthenticated requests")]
    public async Task FoodsEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__ProteinTrackerDatabase");
        var previousSigningKey = Environment.GetEnvironmentVariable("Jwt__SigningKey");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__ProteinTrackerDatabase",
            "Host=unused;Database=unused;Username=unused;Password=unused");
        Environment.SetEnvironmentVariable(
            "Jwt__SigningKey",
            "integration-test-key-that-is-at-least-32-characters");

        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ProteinTrackerDbContext>>();
                    services.RemoveAll<ProteinTrackerDbContext>();
                    services.AddDbContext<ProteinTrackerDbContext>(options =>
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
                });
            });
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.GetAsync("/api/foods");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__ProteinTrackerDatabase", previousConnection);
            Environment.SetEnvironmentVariable("Jwt__SigningKey", previousSigningKey);
        }
    }
}
