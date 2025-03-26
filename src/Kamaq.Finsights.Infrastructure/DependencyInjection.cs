using Kamaq.Finsights.Application.Common.Interfaces;
using Kamaq.Finsights.Application.Common.Interfaces.ExternalServices;
using Kamaq.Finsights.Infrastructure.External.Services;
using Kamaq.Finsights.Infrastructure.Messaging;
using Kamaq.Finsights.Infrastructure.Persistence;
using Kamaq.Finsights.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;

namespace Kamaq.Finsights.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        
        // Register repositories
        // services.AddScoped<IYourRepository, YourRepository>();
        
        // Register services
        services.AddScoped<ICacheService, CacheService>();
        
        // Register external services
        RegisterExternalServices(services);
        
        // Configure MassTransit with RabbitMQ
        services.AddMassTransit(x =>
        {
            // Register consumers here
            // x.AddConsumer<YourConsumer>();
            
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]);
                    h.Password(configuration["RabbitMQ:Password"]);
                });
                
                // Configure endpoints
                // cfg.ReceiveEndpoint("your-queue", e =>
                // {
                //     e.ConfigureConsumer<YourConsumer>(context);
                // });
                
                cfg.ConfigureEndpoints(context);
            });
        });
        
        // Register message publisher
        services.AddScoped<IMessageBusPublisher, MassTransitPublisher>();
        
        // Add memory cache
        services.AddMemoryCache();
        
        return services;
    }
    
    private static void RegisterExternalServices(IServiceCollection services)
    {
        // Register HttpClient factory for Yahoo Finance
        services.AddHttpClient<IYahooFinanceService, YahooFinanceService>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
        
        // Register HttpClient factory for SEC EDGAR
        services.AddHttpClient<ISecEdgarService, SecEdgarService>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
        
        // Register the data aggregator
        services.AddScoped<IDataAggregator, DataAggregator>();
    }
    
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
    
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
} 