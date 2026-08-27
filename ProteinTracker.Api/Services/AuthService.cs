using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProteinTracker.Api.DTOs;
using ProteinTracker.Api.Exceptions;
using ProteinTracker.Api.Models;
using ProteinTracker.Api.Repositories;

namespace ProteinTracker.Api.Services;

public class AuthService(
    UserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    TokenService tokenService,
    TimeProvider timeProvider)
{
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeAndValidateEmail(request.Email);
        ValidatePassword(request.Password);
        var normalizedEmail = NormalizeForComparison(email);

        if (await userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            throw new EmailAlreadyRegisteredException();
        }

        var user = new User
        {
            Email = email,
            NormalizedEmail = normalizedEmail,
            CreatedAt = timeProvider.GetUtcNow()
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        try
        {
            await userRepository.AddAsync(user, cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            throw new EmailAlreadyRegisteredException();
        }
        return tokenService.Create(user);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeForComparison(request.Email);
        var user = await userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password) == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        return tokenService.Create(user);
    }

    private static string NormalizeAndValidateEmail(string? email)
    {
        var trimmed = email?.Trim() ?? string.Empty;
        if (trimmed.Length > 320 || !MailAddress.TryCreate(trimmed, out var parsed) ||
            !string.Equals(parsed.Address, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessValidationException("A valid email address is required.");
        }

        return trimmed;
    }

    private static string NormalizeForComparison(string? email)
    {
        return (email ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new BusinessValidationException("Password must contain at least 8 characters.");
        }
    }
}
