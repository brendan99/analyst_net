using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kamaq.Finsights.Worker.Workers;

public class MessageConsumerBackgroundService : BackgroundService
{
    private readonly ILogger<MessageConsumerBackgroundService> _logger;

    public MessageConsumerBackgroundService(
        ILogger<MessageConsumerBackgroundService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageConsumerBackgroundService started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("MessageConsumerBackgroundService running at: {time}", DateTimeOffset.Now);
            
            try
            {
                // MassTransit handles the consumer registration and execution
                // This service is mostly for logging and ensuring the host stays alive
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when the service is stopping
                _logger.LogInformation("MessageConsumerBackgroundService stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MessageConsumerBackgroundService");
                
                // Add a small delay to avoid CPU spinning on repeated errors
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("MessageConsumerBackgroundService stopped at: {time}", DateTimeOffset.Now);
    }
} 