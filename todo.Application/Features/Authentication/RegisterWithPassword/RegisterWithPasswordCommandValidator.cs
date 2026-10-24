using FluentValidation;
using TodoApi.Domain.Common;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.RegisterWithPassword;

public sealed class RegisterWithPasswordCommandValidator : AbstractValidator<RegisterWithPasswordCommand>
{
    public RegisterWithPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(User.MaximumEmailLength)
            .WithName("E-posta");

        RuleFor(command => command.DisplayName)
            .NotEmpty()
            .MaximumLength(User.MaximumDisplayNameLength)
            .WithName("Ad");

        RuleFor(command => command.Password)
            .NotEmpty()
            .Length(PasswordPolicy.MinimumLength, PasswordPolicy.MaximumLength)
            .Must(PasswordPolicy.ContainsLetterAndDigit)
            .WithMessage("Şifre en az bir harf ve bir rakam içermelidir.")
            .WithName("Şifre");
    }
}
