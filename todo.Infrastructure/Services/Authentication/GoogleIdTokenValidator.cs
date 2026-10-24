using Microsoft.Extensions.Options;
using TodoApi.Infrastructure.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using TodoApi.Application.Abstractions.Authentication;

namespace TodoApi.Infrastructure.Services.Authentication;

public sealed class GoogleIdTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleAuthenticationSettings _googleSettings;
    private readonly ILogger<GoogleIdTokenValidator> _logger;

    public GoogleIdTokenValidator(
        IOptions<GoogleAuthenticationOptions> googleSettings,
        ILogger<GoogleIdTokenValidator> logger)
    {
        if (googleSettings == null)
        {
            throw new ArgumentNullException(nameof(googleSettings));
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        _googleSettings = googleSettings.Value.ToSettings();
        _logger = logger;
    }

    public async Task<GoogleUserInfo?> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        if (_googleSettings.IsEnabled == false)
        {
            _logger.LogWarning("Google sign-in was attempted, but no Google client id is configured.");
            return null;
        }

        GoogleJsonWebSignature.ValidationSettings validationSettings =
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleSettings.ClientId }
            };

        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
        }
        catch (InvalidJwtException exception)
        {
            _logger.LogWarning(exception, "A Google sign-in token was rejected.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
        {
            return null;
        }

        GoogleUserInfo googleUser = new GoogleUserInfo(
            payload.Subject,
            payload.Email,
            payload.EmailVerified,
            payload.Name);

        return googleUser;
    }
}
