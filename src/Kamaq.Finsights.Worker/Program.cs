using Kamaq.Finsights.Application;
using Kamaq.Finsights.Infrastructure;
using Kamaq.Finsights.Worker.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Add Serilog
    builder.Logging.AddSerilog(new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .CreateLogger());

    // Add application layer services
    builder.Services.AddApplication();

    // Add infrastructure layer services
    builder.Services.AddInfrastructure(builder.Configuration);
    
    // Add worker services
    builder.Services.AddHostedService<MessageConsumerBackgroundService>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
