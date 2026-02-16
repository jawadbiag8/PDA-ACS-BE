using DAMS.Application.Interfaces;
using DAMS.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DAMS.API.Services;

/// <summary>Sends SignalR notifications to topic groups so clients can refetch REST data.</summary>
public class DataUpdateNotifier : IDataUpdateNotifier
{
    private readonly IHubContext<DataUpdateHub> _hubContext;
    private readonly ILogger<DataUpdateNotifier> _logger;

    public DataUpdateNotifier(IHubContext<DataUpdateHub> hubContext, ILogger<DataUpdateNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyTopicAsync(string topic, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return;
        try
        {
            await _hubContext.Clients.Group(topic).SendAsync("DataUpdated", topic, cancellationToken);
            _logger.LogInformation("SignalR DataUpdated sent for topic: {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR failed to send DataUpdated for topic: {Topic}", topic);
            throw;
        }
    }
}
