using AiPersona.Domain.Common;
using AiPersona.Domain.Enums;

namespace AiPersona.Domain.Entities;

public class User : BaseEntity
{
    // Identity
    public string Email { get; set; } = null!;
    public string? PasswordHash { get; set; }

    // Firebase/Google Authentication
    public string? FirebaseUid { get; set; }
    public AuthProvider AuthProvider { get; set; } = AuthProvider.Email;
    public string? GoogleId { get; set; }
    public string? DisplayName { get; set; }
    public string? PhotoUrl { get; set; }
    public bool EmailVerified { get; set; }

    // Profile
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; }

    // Google Play Subscriptions
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
    public DateTime? SubscriptionExpiresAt { get; set; }
    public DateTime? GracePeriodEndsAt { get; set; }
    public string? GooglePlayPurchaseToken { get; set; }

    // Navigation properties
    public ICollection<Persona> Personas { get; set; } = new List<Persona>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public ICollection<SubscriptionEvent> SubscriptionEvents { get; set; } = new List<SubscriptionEvent>();
    public ICollection<FcmToken> FcmTokens { get; set; } = new List<FcmToken>();
    public UsageTracking? UsageTracking { get; set; }
    public ICollection<UploadedFile> UploadedFiles { get; set; } = new List<UploadedFile>();
    public ICollection<MarketplacePurchase> MarketplacePurchases { get; set; } = new List<MarketplacePurchase>();

    // Social navigation properties
    public ICollection<PersonaLike> PersonaLikes { get; set; } = new List<PersonaLike>();
    public ICollection<PersonaFavorite> PersonaFavorites { get; set; } = new List<PersonaFavorite>();
    public ICollection<UserFollow> Following { get; set; } = new List<UserFollow>();
    public ICollection<UserFollow> Followers { get; set; } = new List<UserFollow>();
    public ICollection<UserBlock> BlockedUsers { get; set; } = new List<UserBlock>();
    public ICollection<UserBlock> BlockedByUsers { get; set; } = new List<UserBlock>();
    public ICollection<ContentReport> Reports { get; set; } = new List<ContentReport>();
    public ICollection<UserActivity> Activities { get; set; } = new List<UserActivity>();
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();

    // Computed properties
    public bool IsPremium => SubscriptionTier != SubscriptionTier.Free &&
                             (IsInGracePeriod || (SubscriptionExpiresAt.HasValue && DateTime.UtcNow < SubscriptionExpiresAt.Value));

    public bool IsInGracePeriod => GracePeriodEndsAt.HasValue && DateTime.UtcNow < GracePeriodEndsAt.Value;
}
