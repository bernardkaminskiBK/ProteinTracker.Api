using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProteinTracker.Api.Data;
using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Exceptions;
using ProteinTracker.Api.Models;
using ProteinTracker.Api.Repositories;
using ProteinTracker.Api.Security;
using ProteinTracker.Api.Services;
using Xunit;

namespace ProteinTracker.Api.Tests.Services;

public class AuthServiceTests
{
    [Fact(DisplayName = "RegisterAsync creates a user and returns an authentication token")]
    public async Task RegisterAsync_WithValidCredentials_CreatesUser()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var response = await service.RegisterAsync(Credentials("Person@Example.com"));

        var user = await context.Users.SingleAsync();
        Assert.Equal("Person@Example.com", user.Email);
        Assert.Equal("PERSON@EXAMPLE.COM", user.NormalizedEmail);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact(DisplayName = "RegisterAsync rejects an email already registered with different casing")]
    public async Task RegisterAsync_WithDuplicateNormalizedEmail_ThrowsConflict()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.RegisterAsync(Credentials("person@example.com"));

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(
            () => service.RegisterAsync(Credentials(" PERSON@EXAMPLE.COM ")));
    }

    [Fact(DisplayName = "RegisterAsync stores a password hash and never the plaintext password")]
    public async Task RegisterAsync_StoresHashedPassword()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        const string password = "very-secure-password";

        await service.RegisterAsync(Credentials("person@example.com", password));

        var user = await context.Users.SingleAsync();
        Assert.NotEqual(password, user.PasswordHash);
        Assert.DoesNotContain(password, user.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, password));
    }

    [Fact(DisplayName = "LoginAsync returns a token for valid credentials")]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.RegisterAsync(Credentials("person@example.com"));

        var response = await service.LoginAsync(LoginCredentials("PERSON@example.com"));

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("person@example.com", response.Email);
    }

    [Theory(DisplayName = "LoginAsync uses one generic failure for invalid credentials")]
    [InlineData("missing@example.com", "very-secure-password")]
    [InlineData("person@example.com", "wrong-password")]
    public async Task LoginAsync_WithInvalidCredentials_ThrowsGenericException(string email, string password)
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.RegisterAsync(Credentials("person@example.com"));

        var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(LoginCredentials(email, password)));

        Assert.Equal("The email or password is incorrect.", exception.Message);
    }

    private static AuthService CreateService(ProteinTrackerDbContext context)
    {
        var options = Options.Create(new JwtOptions
        {
            SigningKey = "unit-test-signing-key-that-is-at-least-32-characters",
            ExpirationMinutes = 60
        });
        var tokenService = new TokenService(options, TimeProvider.System);
        return new AuthService(
            new UserRepository(context),
            new PasswordHasher<User>(),
            tokenService,
            TimeProvider.System);
    }

    private static RegisterRequest Credentials(string email, string password = "very-secure-password")
    {
        return new RegisterRequest { Email = email, Password = password };
    }

    private static LoginRequest LoginCredentials(string email, string password = "very-secure-password")
    {
        return new LoginRequest { Email = email, Password = password };
    }

    private static ProteinTrackerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProteinTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProteinTrackerDbContext(options);
    }
}
