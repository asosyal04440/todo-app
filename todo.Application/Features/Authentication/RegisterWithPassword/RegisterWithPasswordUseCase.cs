using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using System;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Exceptions;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Domain.Common;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.RegisterWithPassword;

public class RegisterWithPasswordUseCase : UseCase<RegisterWithPasswordCommand, AuthSession>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SessionIssuer _sessionIssuer;

    public RegisterWithPasswordUseCase(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        SessionIssuer sessionIssuer,
        UseCasePipeline<RegisterWithPasswordCommand, AuthSession> pipeline) : base(pipeline)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

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

        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _sessionIssuer = sessionIssuer;
    }

    protected override async Task<Result<AuthSession>> HandleAsync(
        RegisterWithPasswordCommand command, CancellationToken cancellationToken)
    {
        string normalizedEmail = EmailNormalizer.Normalize(command.Email);

        User? existingUser = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (existingUser != null)
        {
            return Result<AuthSession>.Failure(AppErrors.EmailAlreadyRegistered);
        }

        string passwordHash = _passwordHasher.HashPassword(command.Password);

        User newUser = User.RegisterWithPassword(
            normalizedEmail,
            command.DisplayName,
            passwordHash);

        await _userRepository.AddAsync(newUser, cancellationToken);

        PendingAuthSession session = await _sessionIssuer.PrepareAsync(newUser, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthSession>.Success(_sessionIssuer.CreateSession(session));
    }
}
