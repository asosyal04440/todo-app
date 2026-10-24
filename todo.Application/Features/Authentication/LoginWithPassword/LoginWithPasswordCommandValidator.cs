using FluentValidation;
using TodoApi.Domain.Common;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.LoginWithPassword;

public sealed class LoginWithPasswordCommandValidator : AbstractValidator<LoginWithPasswordCommand>
{
    public LoginWithPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(User.MaximumEmailLength)
            .WithName("E-posta");

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(PasswordPolicy.MaximumLength)
            .WithName("Şifre");
    }
}
