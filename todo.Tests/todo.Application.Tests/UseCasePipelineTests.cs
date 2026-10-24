using TodoApi.Application.Common.Results;
using System.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TodoApi.Application;
using TodoApi.Application.Common.Execution;
using TodoApi.Application.Exceptions;
using TodoApi.Application.Features.Todos.CreateTodo;
using todo.Application.Tests.TestDoubles;

namespace todo.Application.Tests;

public class UseCasePipelineTests
{
    [Fact]
    public async Task InvalidCreateTodoNeverReachesTheRepositoryOrSave()
    {
        var repository = new FakeTodoListRepository();
        var list = repository.AddExistingTodoList("Work");
        var log = new RecordingLog();
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddProvider(log));
        var pipeline = new UseCasePipeline<CreateTodoCommand, TodoApi.Application.Contracts.TodoItemDto>(
            [new CreateTodoCommandValidator(FakeTimeProvider.AtTestStart())], factory);
        var useCase = new CreateTodoUseCase(repository.UnitOfWork, repository, FakeCurrentUser.ListOwner(), pipeline);

        var error = await TestResults.FailureAsync(() => useCase.ExecuteAsync(
            new CreateTodoCommand { TodoListId = list.Id, Title = "" }, TestContext.Current.CancellationToken), ErrorKind.Validation);

        Assert.Contains(nameof(CreateTodoCommand.Title), error.Errors.Keys);
        Assert.Empty(list.Todos);
        Assert.Equal(0, repository.UnitOfWork.SaveChangesCount);
        Assert.Contains(log.Messages, message => message.Contains("finished with Rejected"));
        Assert.DoesNotContain(log.Messages, message => message.Contains("finished with Completed"));
        Assert.Equal(0, log.ActiveScopes);
    }

    [Fact]
    public async Task MultipleAsyncValidatorsRunSequentiallyAndAggregateTheirErrors()
    {
        List<string> order = [];
        int active = 0, maximumActive = 0, handlerCalls = 0;
        InlineValidator<Probe> Validator(string field)
        {
            var validator = new InlineValidator<Probe>();
            validator.RuleFor(request => request.Secret).MustAsync(async (_, ct) =>
            {
                maximumActive = Math.Max(maximumActive, ++active);
                await Task.Delay(1, ct);
                order.Add(field);
                --active;
                return false;
            }).OverridePropertyName(field);
            return validator;
        }
        var log = new RecordingLog();
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddProvider(log));
        var pipeline = new UseCasePipeline<Probe, string>([Validator("First"), Validator("Second")], factory);
        IUseCase<Probe, string> useCase = pipeline.Wrap(new ProbeHandler((_, _) =>
        {
            ++handlerCalls;
            return Task.FromResult(Result<string>.Success("not reached"));
        }), "ProbeOperation");

        var error = await TestResults.FailureAsync(() => useCase.ExecuteAsync(
            new Probe("request-secret"), TestContext.Current.CancellationToken), ErrorKind.Validation);

        Assert.Equal(new[] { "First", "Second" }, order);
        Assert.Equal(2, error.Errors.Count);
        Assert.Equal(1, maximumActive);
        Assert.Equal(0, handlerCalls);
    }

    [Fact]
    public async Task ValidRequestIsValidatedOnceThenRunsAndLogsDurationAndTrace()
    {
        int validationCalls = 0, handlerCalls = 0;
        var validator = new InlineValidator<Probe>();
        validator.RuleFor(request => request.Secret).Must(value => { ++validationCalls; return value.Length > 0; });
        var log = new RecordingLog();
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddProvider(log));
        using var activity = new Activity("UseCaseTest").SetIdFormat(ActivityIdFormat.W3C).Start();
        var pipeline = new UseCasePipeline<Probe, string>([validator], factory);
        IUseCase<Probe, string> useCase = pipeline.Wrap(new ProbeHandler((request, ct) =>
        {
            Assert.Equal(1, validationCalls);
            Assert.Equal(TestContext.Current.CancellationToken, ct);
            ++handlerCalls;
            return Task.FromResult(Result<string>.Success("response-secret"));
        }), "ProbeOperation");

        Assert.Equal("response-secret", TestResults.Success(await useCase.ExecuteAsync(new Probe("request-secret"), TestContext.Current.CancellationToken)));
        Assert.Equal(1, handlerCalls);
        Assert.Equal(1, validationCalls);
        Assert.Equal(2, log.Messages.Count);
        Assert.Contains(log.Messages, message => message.Contains("finished with Completed"));
        Assert.Contains(log.Properties, pair => pair.Key == "ElapsedMilliseconds" && (double)pair.Value! >= 0);
        Assert.Contains(log.ScopeProperties, pair => pair.Key == "TraceId" && Equals(pair.Value, activity.TraceId.ToString()));
        Assert.DoesNotContain(log.Messages, message => message.Contains("request-secret") || message.Contains("response-secret"));
        Assert.Equal(0, log.ActiveScopes);
    }

    [Fact]
    public async Task HandlerFailureIsRethrownAndOnlyItsTypeIsLogged()
    {
        var log = new RecordingLog();
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddProvider(log));
        var failure = new InvalidOperationException("password-secret token-secret");
        var pipeline = new UseCasePipeline<Probe, string>([], factory);
        var useCase = pipeline.Wrap(new ProbeHandler((_, _) => Task.FromException<Result<string>>(failure)), "ProbeOperation");

        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(
            new Probe("request-secret"), TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
        Assert.Contains(log.Messages, message => message.Contains(nameof(InvalidOperationException)));
        Assert.DoesNotContain(log.Messages, message => message.Contains("secret"));
        Assert.All(log.Exceptions, exception => Assert.Null(exception));
        Assert.Equal(0, log.ActiveScopes);
    }

    [Fact]
    public async Task CancelledRequestDoesNotRunTheHandlerAndIsLoggedAsCancellation()
    {
        int handlerCalls = 0;
        var log = new RecordingLog();
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddProvider(log));
        var pipeline = new UseCasePipeline<Probe, string>([], factory);
        var useCase = pipeline.Wrap(new ProbeHandler((_, _) => { ++handlerCalls; return Task.FromResult(Result<string>.Success("result")); }), "ProbeOperation");
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(new Probe("secret"), cancelled.Token));
        Assert.Equal(0, handlerCalls);
        Assert.Contains(log.Messages, message => message.Contains("cancelled after"));
        Assert.DoesNotContain(log.Messages, message => message.Contains("failed with"));
        Assert.Equal(0, log.ActiveScopes);
    }

    [Fact]
    public async Task CancellationDuringValidationDoesNotRunTheHandler()
    {
        int handlerCalls = 0;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var validator = new InlineValidator<Probe>();
        validator.RuleFor(request => request.Secret).MustAsync((_, ct) =>
        {
            cancellation.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        });
        var log = new RecordingLog();
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddProvider(log));
        var pipeline = new UseCasePipeline<Probe, string>([validator], factory);
        var useCase = pipeline.Wrap(new ProbeHandler((_, _) => { ++handlerCalls; return Task.FromResult(Result<string>.Success("result")); }), "ProbeOperation");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(new Probe("secret"), cancellation.Token));
        Assert.Equal(0, handlerCalls);
    }

    [Fact]
    public async Task ExpectedFailureLogsItsCodeWithoutExposingItsMessage()
    {
        var log = new RecordingLog();
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddProvider(log));
        var pipeline = new UseCasePipeline<Probe, string>([], factory);
        var useCase = pipeline.Wrap(new ProbeHandler((_, _) => Task.FromResult(Result<string>.Failure(AppErrors.DuplicateName("private-secret")))), "ProbeOperation");
        var error = await TestResults.FailureAsync(() => useCase.ExecuteAsync(new Probe("secret"), TestContext.Current.CancellationToken), ErrorKind.Conflict);
        Assert.Equal(ErrorCode.DuplicateName, error.Code);
        Assert.DoesNotContain(log.Messages, message => message.Contains("secret"));
        Assert.Contains(log.Properties, pair => pair.Key == "ErrorCode" && Equals(pair.Value, ErrorCode.DuplicateName));
        Assert.Contains(log.Messages, message => message.Contains("finished with Rejected"));
    }

    [Fact]
    public void AssemblyRegistrationCoversEveryUseCaseAndCannotBypassTheSharedEntryPoint()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddApplication();
        Type[] implementations = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters
                && type.GetInterfaces().Any(service => service.IsGenericType && service.GetGenericTypeDefinition() == typeof(IUseCase<,>)))
            .ToArray();
        Assert.NotEmpty(implementations);
        foreach (Type implementation in implementations)
        {
            Assert.Single(services, descriptor => descriptor.ServiceType == implementation);
            foreach (Type contract in implementation.GetInterfaces().Where(service => service.IsGenericType
                && service.GetGenericTypeDefinition() == typeof(IUseCase<,>)))
            {
                Assert.Single(services, descriptor => descriptor.ServiceType == contract);
                Type declaringType = Assert.Single(implementation.GetInterfaceMap(contract).TargetMethods).DeclaringType!;
                Assert.True(declaringType.IsGenericType);
                Assert.Equal(typeof(UseCase<,>), declaringType.GetGenericTypeDefinition());
            }
        }
    }

    [Theory]
    [InlineData("duplicate", ErrorCode.DuplicateName, ErrorKind.Conflict)]
    [InlineData("concurrency", ErrorCode.ConcurrentWrite, ErrorKind.Conflict)]
    [InlineData("constraint", ErrorCode.DataConflict, ErrorKind.Conflict)]
    [InlineData("domain", ErrorCode.DomainRuleViolation, ErrorKind.Validation)]
    public async Task ExpectedDomainAndPersistenceErrorsBecomeTypedFailures(string scenario, ErrorCode code, ErrorKind kind)
    {
        Exception error = scenario switch
        {
            "duplicate" => new TodoApi.Domain.Exceptions.DuplicateNameException("Duplicate"),
            "concurrency" => new ConcurrencyConflictException("Changed", new Exception()),
            "constraint" => new ConflictException("Constraint", new Exception()),
            _ => new TodoApi.Domain.Exceptions.DomainRuleViolationException("Invalid")
        };
        var handler = new ProbeHandler((_, _) => Task.FromException<Result<string>>(error));
        var useCase = new ExpectedErrorDecorator<Probe, string>(handler);
        var actual = await TestResults.FailureAsync(() => useCase.ExecuteAsync(new Probe("secret"), TestContext.Current.CancellationToken), kind);
        Assert.Equal(code, actual.Code);
    }

    [Fact]
    public async Task DatabaseOutageRemainsAnException()
    {
        var error = new DataAccessException("Offline", new Exception());
        var useCase = new ExpectedErrorDecorator<Probe, string>(new ProbeHandler((_, _) => Task.FromException<Result<string>>(error)));
        Assert.Same(error, await Assert.ThrowsAsync<DataAccessException>(() => useCase.ExecuteAsync(new Probe("secret"), TestContext.Current.CancellationToken)));
    }

    private sealed record Probe(string Secret);
    private sealed class ProbeHandler(Func<Probe, CancellationToken, Task<Result<string>>> handler) : IUseCase<Probe, string>
    {
        public Task<Result<string>> ExecuteAsync(Probe request, CancellationToken cancellationToken = default) => handler(request, cancellationToken);
    }

    private sealed class RecordingLog : ILoggerProvider, ILogger
    {
        public List<string> Messages { get; } = [];
        public List<Exception?> Exceptions { get; } = [];
        public List<KeyValuePair<string, object?>> Properties { get; } = [];
        public List<KeyValuePair<string, object?>> ScopeProperties { get; } = [];
        public int ActiveScopes { get; private set; }
        public bool IsDisposed { get; private set; }
        public ILogger CreateLogger(string categoryName) => this;
        public bool IsEnabled(LogLevel logLevel) => !IsDisposed;
        public void Dispose() => IsDisposed = true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> values) ScopeProperties.AddRange(values);
            ++ActiveScopes;
            return new LogScope(this);
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            Exceptions.Add(exception);
            if (state is IEnumerable<KeyValuePair<string, object?>> values) Properties.AddRange(values);
        }
        private sealed class LogScope(RecordingLog log) : IDisposable
        {
            private bool _disposed;
            public void Dispose() { if (!_disposed) { --log.ActiveScopes; _disposed = true; } }
        }
    }
}
