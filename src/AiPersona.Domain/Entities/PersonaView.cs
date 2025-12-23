using AiPersona.Domain.Common;

namespace AiPersona.Domain.Entities;

public class PersonaView : BaseEntity
{
    public Guid PersonaId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Persona Persona { get; set; } = null!;
    public User? User { get; set; }
}
