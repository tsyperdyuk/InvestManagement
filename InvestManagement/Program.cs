using FluentValidation;
using InvestManagement.Api.ExceptionHandlers;
using InvestManagement.Application.Common.Interfaces;
using InvestManagement.Application.Portfolios.Interfaces;
using InvestManagement.Application.Portfolios.Services;
using InvestManagement.Application.Portfolios.Validators;
using InvestManagement.Application.Securities.Interfaces;
using InvestManagement.Application.Transactions.Interfaces;
using InvestManagement.Application.Transactions.Services;
using InvestManagement.Infrastructure.Data;
using InvestManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddExceptionHandler<ConcurrencyExceptionHandler>();
builder.Services.AddProblemDetails();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
