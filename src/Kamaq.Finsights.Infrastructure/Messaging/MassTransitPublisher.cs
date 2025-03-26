using Kamaq.Finsights.Application.Common.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Kamaq.Finsights.Infrastructure.Messaging;

public class MassTransitPublisher : IMessageBusPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly ILogger<MassTransitPublisher> _logger;

    public MassTransitPublisher(
        IPublishEndpoint publishEndpoint,
        ISendEndpointProvider sendEndpointProvider,
        ILogger<MassTransitPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _sendEndpointProvider = sendEndpointProvider;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogInformation("Publishing message of type {MessageType}", typeof(T).Name);
        await _publishEndpoint.Publish(message, cancellationToken);
    }

    public async Task SendAsync<T>(T message, string? queue = null, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrEmpty(queue))
        {
            _logger.LogInformation("Publishing message of type {MessageType}", typeof(T).Name);
            await _publishEndpoint.Publish(message, cancellationToken);
            return;
        }

        _logger.LogInformation("Sending message of type {MessageType} to queue {Queue}", typeof(T).Name, queue);
        var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri(queue));
        await endpoint.Send(message, cancellationToken);
    }
} 