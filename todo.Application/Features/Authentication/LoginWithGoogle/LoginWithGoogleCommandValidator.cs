using FluentValidation;

namespace TodoApi.Application.Features.Authentication.LoginWithGoogle;

public sealed class LoginWithGoogleCommandValidator : AbstractValidator<LoginWithGoogleCommand>
{
    private const int IdTokenMaximumLength = 4096;

    public LoginWithGoogleCommandValidator()
    {
        RuleFor(command => command.IdToken)
            .NotEmpty()
            .MaximumLength(IdTokenMaximumLength)
            .WithName("Google kimlik bilgisi");
    }
}
