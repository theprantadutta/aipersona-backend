using AiPersona.Application.Common.Interfaces;

namespace AiPersona.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}
