using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using AiPersona.Application.Common.Interfaces;

namespace AiPersona.Infrastructure.Services;

public class FcmService : IFcmService
{
    private readonly ILogger<FcmService> _logger;

    public FcmService(ILogger<FcmService> logger)
    {
        _logger = logger;
    }

    public async Task SendNotificationAsync(string token, string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            var message = new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };

            await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("FCM notification sent successfully to token: {Token}", token[..20] + "...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM notification to token: {Token}", token[..20] + "...");
            throw;
        }
    }

    public async Task SendMulticastAsync(IEnumerable<string> tokens, string title, string body, Dictionary<string, string>? data = null)
    {
        var tokenList = tokens.ToList();
        if (tokenList.Count == 0) return;

        try
        {
            var message = new MulticastMessage
            {
                Tokens = tokenList,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };

            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
            _logger.LogInformation("FCM multicast sent: {Success}/{Total} successful", response.SuccessCount, tokenList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM multicast notification");
            throw;
        }
    }
}
