using Microsoft.EntityFrameworkCore;
using AiPersona.Domain.Entities;
using AiPersona.Application.Common.Interfaces;

namespace AiPersona.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Core entities
    public DbSet<User> Users => Set<User>();
    public DbSet<UsageTracking> UsageTrackings => Set<UsageTracking>();
    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<KnowledgeBase> KnowledgeBases => Set<KnowledgeBase>();

    // Chat entities
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();

    // Subscription entities
    public DbSet<SubscriptionEvent> SubscriptionEvents => Set<SubscriptionEvent>();

    // File entities
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<FcmToken> FcmTokens => Set<FcmToken>();

    // Social entities
    public DbSet<PersonaLike> PersonaLikes => Set<PersonaLike>();
    public DbSet<PersonaFavorite> PersonaFavorites => Set<PersonaFavorite>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<PersonaView> PersonaViews => Set<PersonaView>();
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();
    public DbSet<UserActivity> UserActivities => Set<UserActivity>();

    // Marketplace entities
    public DbSet<MarketplacePersona> MarketplacePersonas => Set<MarketplacePersona>();
    public DbSet<MarketplacePurchase> MarketplacePurchases => Set<MarketplacePurchase>();
    public DbSet<MarketplaceReview> MarketplaceReviews => Set<MarketplaceReview>();

    // Support entities
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportTicketMessage> SupportTicketMessages => Set<SupportTicketMessage>();
    public DbSet<AutomatedResponse> AutomatedResponses => Set<AutomatedResponse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Apply snake_case naming convention for PostgreSQL compatibility
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Table name
            var tableName = entity.GetTableName();
            if (tableName != null)
            {
                entity.SetTableName(ToSnakeCase(tableName));
            }

            // Column names
            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (columnName != null)
                {
                    property.SetColumnName(ToSnakeCase(columnName));
                }
            }

            // Foreign key names
            foreach (var key in entity.GetForeignKeys())
            {
                var constraintName = key.GetConstraintName();
                if (constraintName != null)
                {
                    key.SetConstraintName(ToSnakeCase(constraintName));
                }
            }

            // Index names
            foreach (var index in entity.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (indexName != null)
                {
                    index.SetDatabaseName(ToSnakeCase(indexName));
                }
            }
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (i > 0 && char.IsUpper(c))
            {
                result.Append('_');
            }
            result.Append(char.ToLower(c));
        }
        return result.ToString();
    }
}
