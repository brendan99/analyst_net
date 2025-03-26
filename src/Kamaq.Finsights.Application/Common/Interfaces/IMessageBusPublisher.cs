using System.Threading.Tasks;

namespace Kamaq.Finsights.Application.Common.Interfaces;

/// <summary>
/// Interface for publishing messages to a message bus
/// </summary>
public interface IMessageBusPublisher
{
    /// <summary>
    /// Publishes a message to all subscribers
    /// </summary>
    /// <typeparam name="T">The type of message to publish</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">A cancellation token</param>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Sends a message to a specific queue
    /// </summary>
    /// <typeparam name="T">The type of message to send</typeparam>
    /// <param name="message">The message to send</param>
    /// <param name="queue">The queue to send to (null for publish)</param>
    /// <param name="cancellationToken">A cancellation token</param>
    Task SendAsync<T>(T message, string? queue = null, CancellationToken cancellationToken = default) where T : class;
} 