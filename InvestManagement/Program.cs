using Azure.Identity;
using Azure.Messaging.ServiceBus;
using FluentValidation;
using InvestManagement.Api.Authentication;
using InvestManagement.Api.ExceptionHandlers;
using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.MarketData;
using InvestManagement.Application.MarketData.Interfaces;
using InvestManagement.Application.MarketData.Services;
using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Portfolios.Services;
using InvestManagement.Application.Portfolios.Validators;
using InvestManagement.Application.Scenarios.Interfaces;
using InvestManagement.Application.Scenarios.Services;
using InvestManagement.Application.Securities.Interfaces;
using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Application.Transactions.Services;
using InvestManagement.Infrastructure.Data;
using InvestManagement.Infrastructure.Identity;
using InvestManagement.Infrastructure.MarketData;
using InvestManagement.Infrastructure.Messaging;
using InvestManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<InvestManagementDbContext>(options =>
     options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddValidatorsFromAssemblyContaining<CreatePortfolioRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ISecurityRepository, SecurityRepository>();
builder.Services.AddScoped<IScenarioService, ScenarioService>();
builder.Services.AddScoped<IScenarioRepository, ScenarioRepository>();
builder.Services.AddSingleton<ScenarioSimulationEngine>();
builder.Services.AddScoped<IPortfolioSummaryService, PortfolioSummaryService>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();    
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
builder.Services.AddExceptionHandler<ConcurrencyExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHostedService<SecurityPriceUpdateWorker>();
builder.Services.AddScoped<TransactionOperationProcessor>();
builder.Services.AddHostedService<TransactionProcessingWorker>();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<IOutboxWriter, OutboxWriter>();
builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddScoped<ITransactionOperationRepository,TransactionOperationRepository>();
builder.Services.Configure<MarketDataOptions>(builder.Configuration.GetSection(MarketDataOptions.SectionName));
builder.Services.Configure<AlphaVantageOptions>(builder.Configuration.GetSection(AlphaVantageOptions.SectionName));
builder.Services.AddHttpClient<IMarketDataProvider, AlphaVantageMarketDataProvider>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<AlphaVantageOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<InvestManagementDbContext>();

builder.Services
    .AddOptions<ServiceBusOptions>()
    .Bind(builder.Configuration.GetSection(ServiceBusOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace),
        "Service Bus namespace is required.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.TopicName),
        "Service Bus topic is required.")
    .Validate(
    options => !string.IsNullOrWhiteSpace(
        options.TransactionProcessingSubscriptionName),
        "Service Bus transaction processing subscription name is required.")
    .ValidateOnStart();

builder.Services.AddSingleton<ServiceBusClient>(serviceProvider =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<ServiceBusOptions>>()
        .Value;

    return new ServiceBusClient(
    options.FullyQualifiedNamespace,
    new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ExcludeManagedIdentityCredential = builder.Environment.IsDevelopment()
    }));
});

builder.Services.AddSingleton<ServiceBusSender>(serviceProvider =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<ServiceBusOptions>>()
        .Value;

    var client = serviceProvider
        .GetRequiredService<ServiceBusClient>();

    return client.CreateSender(options.TopicName);
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

WebApplication app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

    await initializer.InitializeAsync();
}

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/auth")
    .MapIdentityApi<ApplicationUser>();
app.MapGet("/api/auth/me", (ICurrentUser currentUser) =>
{
    return Results.Ok(new
    {
        userId = currentUser.UserId
    });
})
.RequireAuthorization();
if (app.Environment.IsDevelopment())
{
    app.MapPost(
        "/api/dev/service-bus/test",
        async (ServiceBusSender sender, CancellationToken cancellationToken) =>
        {
            var message = new ServiceBusMessage(
                BinaryData.FromObjectAsJson(new
                {
                    Text = "Hello from InvestManagement",
                    CreatedAtUtc = DateTime.UtcNow
                }))
            {
                MessageId = Guid.NewGuid().ToString(),
                Subject = "ConnectionTest",
                ContentType = "application/json"
            };

            await sender.SendMessageAsync(message, cancellationToken);

            return Results.Ok(new
            {
                messageId = message.MessageId,
                status = "Sent"
            });
        })
        .RequireAuthorization();
}
app.MapControllers();

app.Run();

public partial class Program;
