namespace DAMS.Application.Interfaces;

/// <summary>Notifies connected SignalR clients that data for a topic has changed. Clients should refetch the corresponding REST endpoint.</summary>
public interface IDataUpdateNotifier
{
    /// <summary>Notify all clients subscribed to this topic that data changed. They should call the REST API for this topic.</summary>
    Task NotifyTopicAsync(string topic, CancellationToken cancellationToken = default);
}
