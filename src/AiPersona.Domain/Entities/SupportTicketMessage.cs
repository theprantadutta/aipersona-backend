using AiPersona.Domain.Common;

namespace AiPersona.Domain.Entities;

public class SupportTicketMessage : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid SenderId { get; set; }

    // Message content
    public string Content { get; set; } = null!;
    public bool IsStaffReply { get; set; }

    // Attachments (JSON array of file paths)
    public string? Attachments { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public SupportTicket Ticket { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
