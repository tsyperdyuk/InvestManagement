using FluentValidation;
using InvestManagement.Api.ExceptionHandlers;
using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.MarketData;
using InvestManagement.Application.MarketData.Interfaces;
using InvestManagement.Application.MarketData.Services;
using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Portfolios.Services;
using InvestManagement.Application.Portfolios.Validators;
using InvestManagement.Application.Securities.Interfaces;
using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Application.Transactions.Services;
using InvestManagement.Infrastructure.Data;
using InvestManagement.Infrastructure.MarketData;
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
builder.Services.AddScoped<IPortfolioSummaryService, PortfolioSummaryService>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();    
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddExceptionHandler<ConcurrencyExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHostedService<SecurityPriceUpdateWorker>();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.Configure<MarketDataOptions>(builder.Configuration.GetSection(MarketDataOptions.SectionName));
builder.Services.Configure<AlphaVantageOptions>(builder.Configuration.GetSection(AlphaVantageOptions.SectionName));
builder.Services.AddHttpClient<IMarketDataProvider, AlphaVantageMarketDataProvider>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<AlphaVantageOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl);
});

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

app.MapControllers();

app.Run();
