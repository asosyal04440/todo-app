using System.Text.Json.Serialization;
using Serilog;
using Serilog.Events;
using TodoApi.API.Authentication;
using TodoApi.API.BackgroundJobs;
using TodoApi.API.ExceptionHandling;
using TodoApi.API.Security;
using TodoApi.Application;
using TodoApi.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogEventLevel.Fatal));

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
    });

    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = ProblemDetailsWriter.GetTraceId(context.HttpContext);
        };
    });

    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();   
    builder.Services.AddExceptionHandler<DomainExceptionHandler>();       
    builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();  
    builder.Services.AddExceptionHandler<DatabaseExceptionHandler>();      
    builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();    

    string[] allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .GetChildren()
        .Select(child => child.Value)
        .Where(origin => string.IsNullOrWhiteSpace(origin) == false)
        .Select(origin => origin!)
        .ToArray();

    if (allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException("At least one Cors:AllowedOrigins entry is required.");
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("TodoWeb", policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .WithHeaders("Content-Type", "Authorization", RequireClientHeaderAttribute.HeaderName)
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .AllowCredentials();
        });
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddTodoAuthentication();

    builder.Services.AddTodoBackgroundJobs(builder.Configuration);

    var app = builder.Build();

    app.UseSecurityHeaders();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0} ms (user {UserId})";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? "anonymous");
        };
    });

    if (app.Environment.IsProduction())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseExceptionHandler();
    app.UseCors("TodoWeb");

    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseTodoBackgroundJobs();
    app.MapControllers();

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "The application failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program
{
}
