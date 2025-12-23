using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiPersona.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "automated_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    keywords = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automated_responses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    firebase_uid = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    auth_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    google_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    photo_url = table.Column<string>(type: "text", nullable: true),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false),
                    subscription_tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subscription_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    grace_period_ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    google_play_purchase_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "content_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    additional_info = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_reports", x => x.id);
                    table.ForeignKey(
                        name: "f_k_content_reports__users_reporter_id",
                        column: x => x.reporter_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fcm_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    device_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fcm_tokens", x => x.id);
                    table.ForeignKey(
                        name: "f_k_fcm_tokens__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "personas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    image_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    personality_traits = table.Column<string>(type: "jsonb", nullable: true),
                    language_style = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    expertise = table.Column<string>(type: "jsonb", nullable: true),
                    tags = table.Column<string>(type: "jsonb", nullable: true),
                    voice_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    voice_settings = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    is_marketplace = table.Column<bool>(type: "boolean", nullable: false),
                    conversation_count = table.Column<int>(type: "integer", nullable: false),
                    clone_count = table.Column<int>(type: "integer", nullable: false),
                    like_count = table.Column<int>(type: "integer", nullable: false),
                    cloned_from_persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_creator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personas", x => x.id);
                    table.ForeignKey(
                        name: "f_k_personas__users_creator_id",
                        column: x => x.creator_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_personas__users_original_creator_id",
                        column: x => x.original_creator_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_personas_personas_cloned_from_persona_id",
                        column: x => x.cloned_from_persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "subscription_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subscription_tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    verification_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    raw_response = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_events", x => x.id);
                    table.ForeignKey(
                        name: "f_k_subscription_events__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "support_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_tickets", x => x.id);
                    table.ForeignKey(
                        name: "f_k_support_tickets__users_assigned_to",
                        column: x => x.assigned_to,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_support_tickets__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "uploaded_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    original_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_size = table.Column<int>(type: "integer", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uploaded_files", x => x.id);
                    table.ForeignKey(
                        name: "f_k_uploaded_files__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usage_trackings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    messages_today = table.Column<int>(type: "integer", nullable: false),
                    messages_count_reset_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    personas_count = table.Column<int>(type: "integer", nullable: false),
                    storage_used_bytes = table.Column<long>(type: "bigint", nullable: false),
                    gemini_api_calls_today = table.Column<int>(type: "integer", nullable: false),
                    gemini_tokens_used_total = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_trackings", x => x.id);
                    table.ForeignKey(
                        name: "f_k_usage_trackings__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    activity_data = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_activities", x => x.id);
                    table.ForeignKey(
                        name: "f_k_user_activities_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blocked_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_blocks", x => x.id);
                    table.ForeignKey(
                        name: "f_k_user_blocks_users_blocked_id",
                        column: x => x.blocked_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_user_blocks_users_blocker_id",
                        column: x => x.blocker_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_follows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    follower_id = table.Column<Guid>(type: "uuid", nullable: false),
                    following_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_follows", x => x.id);
                    table.ForeignKey(
                        name: "f_k_user_follows_users_follower_id",
                        column: x => x.follower_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_user_follows_users_following_id",
                        column: x => x.following_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    persona_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    deleted_persona_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    deleted_persona_image = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    persona_deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    message_count = table.Column<int>(type: "integer", nullable: false),
                    meta_data = table.Column<string>(type: "jsonb", nullable: true),
                    last_message_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_sessions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_chat_sessions__personas_persona_id",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_chat_sessions__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_bases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    tokens = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    indexed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    meta_data = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_bases", x => x.id);
                    table.ForeignKey(
                        name: "f_k_knowledge_bases__personas_persona_id",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marketplace_personas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pricing_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    purchase_count = table.Column<int>(type: "integer", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketplace_personas", x => x.id);
                    table.ForeignKey(
                        name: "f_k_marketplace_personas__personas_persona_id",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_marketplace_personas__users_seller_id",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "persona_favorites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persona_favorites", x => x.id);
                    table.ForeignKey(
                        name: "f_k_persona_favorites__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_persona_favorites_personas_persona_id",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "persona_likes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persona_likes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_persona_likes__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_persona_likes_personas_persona_id",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "persona_views",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persona_views", x => x.id);
                    table.ForeignKey(
                        name: "f_k_persona_views__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_persona_views_personas_persona_id",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "support_ticket_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    is_staff_reply = table.Column<bool>(type: "boolean", nullable: false),
                    attachments = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_ticket_messages", x => x.id);
                    table.ForeignKey(
                        name: "f_k_support_ticket_messages__users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_support_ticket_messages_support_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "support_tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    message_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sentiment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tokens_used = table.Column<int>(type: "integer", nullable: false),
                    meta_data = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "f_k_chat_messages__chat_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marketplace_purchases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    purchased_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketplace_purchases", x => x.id);
                    table.ForeignKey(
                        name: "f_k_marketplace_purchases__users_buyer_id",
                        column: x => x.buyer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_marketplace_purchases_marketplace_personas_marketplace_pers~",
                        column: x => x.marketplace_persona_id,
                        principalTable: "marketplace_personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marketplace_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    review_text = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketplace_reviews", x => x.id);
                    table.ForeignKey(
                        name: "f_k_marketplace_reviews__users_reviewer_id",
                        column: x => x.reviewer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_marketplace_reviews_marketplace_personas_marketplace_person~",
                        column: x => x.marketplace_persona_id,
                        principalTable: "marketplace_personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_size = table.Column<int>(type: "integer", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    attachment_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_attachments", x => x.id);
                    table.ForeignKey(
                        name: "f_k_message_attachments_chat_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_automated_responses_category",
                table: "automated_responses",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "i_x_automated_responses_is_active",
                table: "automated_responses",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "i_x_automated_responses_priority",
                table: "automated_responses",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "i_x_chat_messages_created_at",
                table: "chat_messages",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "i_x_chat_messages_session_id_created_at",
                table: "chat_messages",
                columns: new[] { "session_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "i_x_chat_sessions_last_message_at",
                table: "chat_sessions",
                column: "last_message_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "i_x_chat_sessions_persona_id",
                table: "chat_sessions",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "i_x_chat_sessions_user_id_is_pinned_last_message_at",
                table: "chat_sessions",
                columns: new[] { "user_id", "is_pinned", "last_message_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "i_x_chat_sessions_user_id_status",
                table: "chat_sessions",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_content_reports_content_id",
                table: "content_reports",
                column: "content_id");

            migrationBuilder.CreateIndex(
                name: "i_x_content_reports_content_type",
                table: "content_reports",
                column: "content_type");

            migrationBuilder.CreateIndex(
                name: "i_x_content_reports_created_at",
                table: "content_reports",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "i_x_content_reports_reporter_id",
                table: "content_reports",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "i_x_content_reports_status",
                table: "content_reports",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_fcm_tokens_device_id",
                table: "fcm_tokens",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "i_x_fcm_tokens_is_active",
                table: "fcm_tokens",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "i_x_fcm_tokens_token",
                table: "fcm_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_fcm_tokens_user_id",
                table: "fcm_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_knowledge_bases_persona_id",
                table: "knowledge_bases",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "i_x_knowledge_bases_status",
                table: "knowledge_bases",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_marketplace_personas_category",
                table: "marketplace_personas",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "i_x_marketplace_personas_persona_id",
                table: "marketplace_personas",
                column: "persona_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_marketplace_personas_seller_id",
                table: "marketplace_personas",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "i_x_marketplace_personas_status",
                table: "marketplace_personas",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_marketplace_purchases_buyer_id",
                table: "marketplace_purchases",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "i_x_marketplace_purchases_marketplace_persona_id",
                table: "marketplace_purchases",
                column: "marketplace_persona_id");

            migrationBuilder.CreateIndex(
                name: "i_x_marketplace_purchases_purchased_at",
                table: "marketplace_purchases",
                column: "purchased_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "i_x_marketplace_reviews_marketplace_persona_id",
                table: "marketplace_reviews",
                column: "marketplace_persona_id");

            migrationBuilder.CreateIndex(
                name: "i_x_marketplace_reviews_marketplace_persona_id_reviewer_id",
                table: "marketplace_reviews",
                columns: new[] { "marketplace_persona_id", "reviewer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_marketplace_reviews_rating",
                table: "marketplace_reviews",
                column: "rating");

            migrationBuilder.CreateIndex(
                name: "i_x_marketplace_reviews_reviewer_id",
                table: "marketplace_reviews",
                column: "reviewer_id");

            migrationBuilder.CreateIndex(
                name: "i_x_message_attachments_message_id",
                table: "message_attachments",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "i_x_persona_favorites_persona_id",
                table: "persona_favorites",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "i_x_persona_favorites_user_id",
                table: "persona_favorites",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_persona_favorites_user_id_persona_id",
                table: "persona_favorites",
                columns: new[] { "user_id", "persona_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_persona_likes_persona_id",
                table: "persona_likes",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "i_x_persona_likes_user_id",
                table: "persona_likes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_persona_likes_user_id_persona_id",
                table: "persona_likes",
                columns: new[] { "user_id", "persona_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_persona_views_persona_id",
                table: "persona_views",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "i_x_persona_views_user_id",
                table: "persona_views",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_persona_views_viewed_at",
                table: "persona_views",
                column: "viewed_at");

            migrationBuilder.CreateIndex(
                name: "i_x_personas_cloned_from_persona_id",
                table: "personas",
                column: "cloned_from_persona_id");

            migrationBuilder.CreateIndex(
                name: "i_x_personas_created_at",
                table: "personas",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "i_x_personas_creator_id",
                table: "personas",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "i_x_personas_is_marketplace",
                table: "personas",
                column: "is_marketplace");

            migrationBuilder.CreateIndex(
                name: "i_x_personas_is_public_status",
                table: "personas",
                columns: new[] { "is_public", "status" });

            migrationBuilder.CreateIndex(
                name: "i_x_personas_like_count",
                table: "personas",
                column: "like_count",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "i_x_personas_original_creator_id",
                table: "personas",
                column: "original_creator_id");

            migrationBuilder.CreateIndex(
                name: "i_x_subscription_events_created_at",
                table: "subscription_events",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "i_x_subscription_events_purchase_token",
                table: "subscription_events",
                column: "purchase_token");

            migrationBuilder.CreateIndex(
                name: "i_x_subscription_events_user_id_created_at",
                table: "subscription_events",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "i_x_support_ticket_messages_created_at",
                table: "support_ticket_messages",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "i_x_support_ticket_messages_sender_id",
                table: "support_ticket_messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "i_x_support_ticket_messages_ticket_id",
                table: "support_ticket_messages",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "i_x_support_tickets_assigned_to",
                table: "support_tickets",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "i_x_support_tickets_created_at",
                table: "support_tickets",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "i_x_support_tickets_priority",
                table: "support_tickets",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "i_x_support_tickets_status",
                table: "support_tickets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_support_tickets_user_id",
                table: "support_tickets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_uploaded_files_category",
                table: "uploaded_files",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "i_x_uploaded_files_reference_type_reference_id",
                table: "uploaded_files",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "i_x_uploaded_files_user_id",
                table: "uploaded_files",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_usage_trackings_user_id",
                table: "usage_trackings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_user_activities_activity_type",
                table: "user_activities",
                column: "activity_type");

            migrationBuilder.CreateIndex(
                name: "i_x_user_activities_created_at",
                table: "user_activities",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "i_x_user_activities_target_id",
                table: "user_activities",
                column: "target_id");

            migrationBuilder.CreateIndex(
                name: "i_x_user_activities_user_id",
                table: "user_activities",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_user_blocks_blocked_id",
                table: "user_blocks",
                column: "blocked_id");

            migrationBuilder.CreateIndex(
                name: "i_x_user_blocks_blocker_id",
                table: "user_blocks",
                column: "blocker_id");

            migrationBuilder.CreateIndex(
                name: "i_x_user_blocks_blocker_id_blocked_id",
                table: "user_blocks",
                columns: new[] { "blocker_id", "blocked_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_user_follows_follower_id",
                table: "user_follows",
                column: "follower_id");

            migrationBuilder.CreateIndex(
                name: "i_x_user_follows_follower_id_following_id",
                table: "user_follows",
                columns: new[] { "follower_id", "following_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_user_follows_following_id",
                table: "user_follows",
                column: "following_id");

            migrationBuilder.CreateIndex(
                name: "i_x_users_created_at",
                table: "users",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "i_x_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_users_firebase_uid",
                table: "users",
                column: "firebase_uid",
                unique: true,
                filter: "firebase_uid IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "i_x_users_google_id",
                table: "users",
                column: "google_id",
                unique: true,
                filter: "google_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "i_x_users_is_active",
                table: "users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "i_x_users_subscription_tier",
                table: "users",
                column: "subscription_tier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "automated_responses");

            migrationBuilder.DropTable(
                name: "content_reports");

            migrationBuilder.DropTable(
                name: "fcm_tokens");

            migrationBuilder.DropTable(
                name: "knowledge_bases");

            migrationBuilder.DropTable(
                name: "marketplace_purchases");

            migrationBuilder.DropTable(
                name: "marketplace_reviews");

            migrationBuilder.DropTable(
                name: "message_attachments");

            migrationBuilder.DropTable(
                name: "persona_favorites");

            migrationBuilder.DropTable(
                name: "persona_likes");

            migrationBuilder.DropTable(
                name: "persona_views");

            migrationBuilder.DropTable(
                name: "subscription_events");

            migrationBuilder.DropTable(
                name: "support_ticket_messages");

            migrationBuilder.DropTable(
                name: "uploaded_files");

            migrationBuilder.DropTable(
                name: "usage_trackings");

            migrationBuilder.DropTable(
                name: "user_activities");

            migrationBuilder.DropTable(
                name: "user_blocks");

            migrationBuilder.DropTable(
                name: "user_follows");

            migrationBuilder.DropTable(
                name: "marketplace_personas");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "support_tickets");

            migrationBuilder.DropTable(
                name: "chat_sessions");

            migrationBuilder.DropTable(
                name: "personas");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
