using AiPersona.Domain.Common;

namespace AiPersona.Domain.Entities;

/// <summary>
/// Stores the greeting message for a user+persona combination.
/// Greetings are generated only once per user per persona and reused for all future sessions.
/// </summary>
public class PersonaGreeting : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid PersonaId { get; set; }
    public string GreetingText { get; set; } = null!;
    public int TokensUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Persona Persona { get; set; } = null!;
}
