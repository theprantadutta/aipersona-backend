using AiPersona.Domain.Common;

namespace AiPersona.Domain.Entities;

public class PersonaLike : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid PersonaId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Persona Persona { get; set; } = null!;
}
