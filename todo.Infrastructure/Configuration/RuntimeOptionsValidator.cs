using Microsoft.Extensions.Options;

namespace TodoApi.Infrastructure.Configuration;

public sealed class RuntimeOptionsValidator : IValidateOptions<JwtOptions>, IValidateOptions<SmtpOptions>,
    IValidateOptions<ReminderOptions>, IValidateOptions<SecurityOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options) => ValidateConstruction(() =>
    {
        options.ToSettings();
        if (options.RefreshTokenLifetimeDays <= 0)
            throw new InvalidOperationException("The Jwt:RefreshTokenLifetimeDays setting must be a positive whole number.");
        options.ToRefreshTokenSettings();
    });

    public ValidateOptionsResult Validate(string? name, SmtpOptions options) => ValidateConstruction(() =>
    {
        if (!Enum.IsDefined(options.Security))
            throw new InvalidOperationException("The Email:Smtp:Security setting must be StartTls, SslOnConnect or None.");
        options.ToSettings();
    });

    public ValidateOptionsResult Validate(string? name, ReminderOptions options) =>
        ValidateConstruction(() => options.ToSettings());

    public ValidateOptionsResult Validate(string? name, SecurityOptions options) =>
        options.AuthenticationRequestsPerMinute > 0 ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("The Security:AuthenticationRequestsPerMinute setting must be a positive whole number.");

    private static ValidateOptionsResult ValidateConstruction(Action validate)
    {
        try
        {
            validate();
            return ValidateOptionsResult.Success;
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException
            or OverflowException or TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return ValidateOptionsResult.Fail(exception.Message);
        }
    }
}
