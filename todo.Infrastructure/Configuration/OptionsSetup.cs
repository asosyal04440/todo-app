using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace TodoApi.Infrastructure.Configuration;

public static class OptionsSetup
{
    public static IServiceCollection AddTodoOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<JwtOptions>, RuntimeOptionsValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmtpOptions>, RuntimeOptionsValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ReminderOptions>, RuntimeOptionsValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SecurityOptions>, RuntimeOptionsValidator>());

        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateOnStart();
        services.AddOptions<SmtpOptions>().Bind(configuration.GetSection(SmtpOptions.SectionName)).ValidateOnStart();
        services.AddOptions<ReminderOptions>().Bind(configuration.GetSection(ReminderOptions.SectionName)).ValidateOnStart();
        services.AddOptions<SecurityOptions>().Bind(configuration.GetSection(SecurityOptions.SectionName)).ValidateOnStart();
        services.AddOptions<GoogleAuthenticationOptions>().Bind(configuration.GetSection(GoogleAuthenticationOptions.SectionName)).ValidateOnStart();
        services.AddOptions<AuthorizationOptions>().Bind(configuration.GetSection(AuthorizationOptions.SectionName)).ValidateOnStart();
        return services;
    }
}
