using Microsoft.Extensions.DependencyInjection;
using TodoApi.Application.Common.Execution;

namespace todo.API.Tests;

public class UseCaseRegistrationTests
{
    [Fact]
    public async Task EveryUseCaseAndItsContractResolveToTheSameScopedPipelineEntryPoint()
    {
        using var factory = new TodoApiFactory();
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        Type[] implementations = typeof(TodoApi.Application.DependencyInjection).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters
                && type.GetInterfaces().Any(service => service.IsGenericType
                    && service.GetGenericTypeDefinition() == typeof(IUseCase<,>))).ToArray();

        Assert.NotEmpty(implementations);
        foreach (Type implementation in implementations)
        {
            object concrete = scope.ServiceProvider.GetRequiredService(implementation);
            foreach (Type contract in implementation.GetInterfaces().Where(service => service.IsGenericType
                && service.GetGenericTypeDefinition() == typeof(IUseCase<,>)))
                Assert.Same(concrete, scope.ServiceProvider.GetRequiredService(contract));
        }
    }
}
