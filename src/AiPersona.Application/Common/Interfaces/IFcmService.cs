namespace AiPersona.Application.Common.Interfaces;

public interface IFcmService
{
    Task SendNotificationAsync(string token, string title, string body, Dictionary<string, string>? data = null);
    Task SendMulticastAsync(IEnumerable<string> tokens, string title, string body, Dictionary<string, string>? data = null);
}
