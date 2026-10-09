using System.Linq;
using FluentValidation;

namespace TodoApi.Application.Features.Authentication.RegisterWithPassword;

public sealed class RegisterWithPasswordCommandValidator : AbstractValidator<RegisterWithPasswordCommand>
{
    public const int PasswordMinimumLength = 8;
    public const int PasswordMaximumLength = 128;

    public RegisterWithPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256)
            .WithName("E-posta");

        RuleFor(command => command.DisplayName)
            .NotEmpty()
            .MaximumLength(100)
            .WithName("Ad");

        RuleFor(command => command.Password)
            .NotEmpty()
            .Length(PasswordMinimumLength, PasswordMaximumLength)
            .Must(password => password.Any(char.IsLetter) && password.Any(char.IsDigit))
            .WithMessage("Şifre en az bir harf ve bir rakam içermelidir.")
            .WithName("Şifre");
    }
}
