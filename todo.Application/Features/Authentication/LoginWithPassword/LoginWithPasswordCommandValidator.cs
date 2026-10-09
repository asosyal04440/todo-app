using FluentValidation;

namespace TodoApi.Application.Features.Authentication.LoginWithPassword;

public sealed class LoginWithPasswordCommandValidator : AbstractValidator<LoginWithPasswordCommand>
{
    public LoginWithPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(256)
            .WithName("E-posta");

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(128)
            .WithName("Şifre");
    }
}
