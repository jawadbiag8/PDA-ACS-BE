using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DAMS.API.Hubs;

/// <summary>SignalR hub for real-time data-update notifications. Client joins topics; server sends DataUpdated(topic) when data changes.</summary>
[Authorize(Roles = "PDA Analyst,PMO Executive")]
public class DataUpdateHub : Hub
{
    /// <summary>Client calls this to subscribe to a topic. When server calls NotifyTopic(topic), all clients in this group receive DataUpdated(topic).</summary>
    public async Task JoinTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return;
        await Groups.AddToGroupAsync(Context.ConnectionId, topic);
    }

    /// <summary>Client calls this to unsubscribe from a topic.</summary>
    public async Task LeaveTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, topic);
    }
}
