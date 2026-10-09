using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Common.Validation;
using TodoApi.Application.Exceptions;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.LoginWithPassword;

public class LoginWithPasswordUseCase
{
    private const string InvalidCredentialsMessage = "The email address or password is incorrect.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SessionIssuer _sessionIssuer;
    private readonly IValidator<LoginWithPasswordCommand> _validator;

    public LoginWithPasswordUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        SessionIssuer sessionIssuer,
        IValidator<LoginWithPasswordCommand> validator)
    {
        if (userRepository == null)
        {
            throw new ArgumentNullException(nameof(userRepository));
        }

        if (passwordHasher == null)
        {
            throw new ArgumentNullException(nameof(passwordHasher));
        }

        if (sessionIssuer == null)
        {
            throw new ArgumentNullException(nameof(sessionIssuer));
        }

        if (validator == null)
        {
            throw new ArgumentNullException(nameof(validator));
        }

        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _sessionIssuer = sessionIssuer;
        _validator = validator;
    }

    public async Task<AuthSession> ExecuteAsync(
        LoginWithPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateOrThrowAsync(command, cancellationToken);

        string normalizedEmail = EmailNormalizer.Normalize(command.Email);
        string providedPassword = command.Password;

        User? user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user == null || user.PasswordHash == null)
        {
            _passwordHasher.HashPassword(providedPassword);

            throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }

        PasswordVerificationOutcome outcome = _passwordHasher.VerifyPassword(
            user.PasswordHash,
            providedPassword);

        if (outcome == PasswordVerificationOutcome.Failed)
        {
            throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }

        if (outcome == PasswordVerificationOutcome.SucceededRehashNeeded)
        {
            user.ReplacePasswordHash(_passwordHasher.HashPassword(providedPassword));
        }

        await _userRepository.SaveChangesAsync(cancellationToken);

        AuthSession session = await _sessionIssuer.IssueAsync(user, cancellationToken);

        return session;
    }
}
