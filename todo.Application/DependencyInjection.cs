using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApi.Application.Common.Execution;
using TodoApi.Application.Features.Authentication.Common;

namespace TodoApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("tr-TR");
        services.AddLogging();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.TryAddScoped(typeof(UseCasePipeline<,>));
        services.TryAddScoped<SessionIssuer>();

        Type contract = typeof(IUseCase<,>);
        var implementations = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
            .SelectMany(type => type.GetInterfaces()
                .Where(service => service.IsGenericType && service.GetGenericTypeDefinition() == contract)
                .Select(service => (Implementation: type, Service: service)))
            .ToArray();

        foreach (var group in implementations.GroupBy(item => item.Service))
        {
            if (group.Count() != 1)
                throw new InvalidOperationException($"Multiple use cases implement {group.Key}.");
            var item = group.Single();
            services.TryAddScoped(item.Implementation);
            services.TryAddScoped(item.Service, provider => provider.GetRequiredService(item.Implementation));
        }

        return services;
    }
}
