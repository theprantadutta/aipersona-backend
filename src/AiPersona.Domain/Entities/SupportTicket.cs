using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class SupportTicket : AuditableEntity
{
    public Guid UserId { get; set; }

    // Ticket information
    public string Subject { get; set; } = null!;
    public TicketCategory Category { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    // Assignment
    public Guid? AssignedToId { get; set; }
    public DateTime? AssignedAt { get; set; }

    // Resolution
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public User? AssignedTo { get; set; }
    public ICollection<SupportTicketMessage> Messages { get; set; } = new List<SupportTicketMessage>();
}
