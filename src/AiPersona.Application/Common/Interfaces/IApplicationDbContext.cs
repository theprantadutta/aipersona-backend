using Microsoft.EntityFrameworkCore;
using AiPersona.Domain.Entities;

namespace AiPersona.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Core entities
    DbSet<User> Users { get; }
    DbSet<UsageTracking> UsageTrackings { get; }
    DbSet<Persona> Personas { get; }
    DbSet<KnowledgeBase> KnowledgeBases { get; }

    // Chat entities
    DbSet<ChatSession> ChatSessions { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<MessageAttachment> MessageAttachments { get; }

    // Subscription entities
    DbSet<SubscriptionEvent> SubscriptionEvents { get; }

    // File entities
    DbSet<UploadedFile> UploadedFiles { get; }
    DbSet<FcmToken> FcmTokens { get; }

    // Social entities
    DbSet<PersonaLike> PersonaLikes { get; }
    DbSet<PersonaFavorite> PersonaFavorites { get; }
    DbSet<UserFollow> UserFollows { get; }
    DbSet<UserBlock> UserBlocks { get; }
    DbSet<PersonaView> PersonaViews { get; }
    DbSet<ContentReport> ContentReports { get; }
    DbSet<UserActivity> UserActivities { get; }

    // Marketplace entities
    DbSet<MarketplacePersona> MarketplacePersonas { get; }
    DbSet<MarketplacePurchase> MarketplacePurchases { get; }
    DbSet<MarketplaceReview> MarketplaceReviews { get; }

    // Support entities
    DbSet<SupportTicket> SupportTickets { get; }
    DbSet<SupportTicketMessage> SupportTicketMessages { get; }
    DbSet<AutomatedResponse> AutomatedResponses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
