using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using System;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Domain.Common;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.LoginWithPassword;

public class LoginWithPasswordUseCase : UseCase<LoginWithPasswordCommand, AuthSession>
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SessionIssuer _sessionIssuer;

    public LoginWithPasswordUseCase(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        SessionIssuer sessionIssuer,
        UseCasePipeline<LoginWithPasswordCommand, AuthSession> pipeline) : base(pipeline)
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
        LoginWithPasswordCommand command, CancellationToken cancellationToken)
    {
        string normalizedEmail = EmailNormalizer.Normalize(command.Email);
        string providedPassword = command.Password;

        User? user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user == null || user.PasswordHash == null)
        {
            _passwordHasher.HashPassword(providedPassword);

            return Result<AuthSession>.Failure(AppErrors.InvalidCredentials);
        }

        PasswordVerificationOutcome outcome = _passwordHasher.VerifyPassword(
            user.PasswordHash,
            providedPassword);

        if (outcome == PasswordVerificationOutcome.Failed)
        {
            return Result<AuthSession>.Failure(AppErrors.InvalidCredentials);
        }

        if (outcome == PasswordVerificationOutcome.SucceededRehashNeeded)
        {
            user.ReplacePasswordHash(_passwordHasher.HashPassword(providedPassword));
        }

        PendingAuthSession session = await _sessionIssuer.PrepareAsync(user, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthSession>.Success(_sessionIssuer.CreateSession(session));
    }
}
