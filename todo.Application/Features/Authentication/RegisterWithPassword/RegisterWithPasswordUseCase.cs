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

namespace TodoApi.Application.Features.Authentication.RegisterWithPassword;

public class RegisterWithPasswordUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SessionIssuer _sessionIssuer;
    private readonly IValidator<RegisterWithPasswordCommand> _validator;

    public RegisterWithPasswordUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        SessionIssuer sessionIssuer,
        IValidator<RegisterWithPasswordCommand> validator)
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
        RegisterWithPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateOrThrowAsync(command, cancellationToken);

        string normalizedEmail = EmailNormalizer.Normalize(command.Email);

        User? existingUser = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (existingUser != null)
        {
            throw new EmailAlreadyRegisteredException();
        }

        string passwordHash = _passwordHasher.HashPassword(command.Password);

        User newUser = User.RegisterWithPassword(
            normalizedEmail,
            command.DisplayName,
            passwordHash);

        await _userRepository.AddAsync(newUser, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        AuthSession session = await _sessionIssuer.IssueAsync(newUser, cancellationToken);

        return session;
    }
}
