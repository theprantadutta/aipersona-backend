using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiPersona.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixUserActivityMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_support_tickets__users_assigned_to",
                table: "support_tickets");

            migrationBuilder.DropColumn(
                name: "description",
                table: "support_tickets");

            migrationBuilder.DropColumn(
                name: "last_used_at",
                table: "fcm_tokens");

            migrationBuilder.RenameColumn(
                name: "activity_data",
                table: "user_activities",
                newName: "metadata");

            migrationBuilder.RenameColumn(
                name: "assigned_to",
                table: "support_tickets",
                newName: "assigned_to_id");

            migrationBuilder.RenameIndex(
                name: "i_x_support_tickets_assigned_to",
                table: "support_tickets",
                newName: "i_x_support_tickets_assigned_to_id");

            migrationBuilder.RenameColumn(
                name: "reviewed_by",
                table: "content_reports",
                newName: "resolved_by_id");

            migrationBuilder.RenameColumn(
                name: "reviewed_at",
                table: "content_reports",
                newName: "resolved_at");

            migrationBuilder.RenameColumn(
                name: "additional_info",
                table: "content_reports",
                newName: "description");

            migrationBuilder.AddColumn<int>(
                name: "follower_count",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "following_count",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_suspended",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_active_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "profile_image",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "suspended_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "suspended_until",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suspension_reason",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Use raw SQL for type conversion from varchar to uuid
            migrationBuilder.Sql(@"
                ALTER TABLE user_activities
                ALTER COLUMN target_id TYPE uuid
                USING CASE WHEN target_id ~ '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
                    THEN target_id::uuid
                    ELSE NULL
                END;
            ");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "user_activities",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "favorite_count",
                table: "personas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "prompt",
                table: "personas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "view_count",
                table: "personas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "device_name",
                table: "fcm_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_active_at",
                table: "fcm_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reason",
                table: "content_reports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            // Use raw SQL for type conversion from varchar to uuid
            migrationBuilder.Sql(@"
                ALTER TABLE content_reports
                ALTER COLUMN content_id TYPE uuid
                USING CASE WHEN content_id ~ '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
                    THEN content_id::uuid
                    ELSE '00000000-0000-0000-0000-000000000000'::uuid
                END;
            ");

            migrationBuilder.CreateIndex(
                name: "i_x_content_reports_resolved_by_id",
                table: "content_reports",
                column: "resolved_by_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_content_reports__users_resolved_by_id",
                table: "content_reports",
                column: "resolved_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "f_k_support_tickets__users_assigned_to_id",
                table: "support_tickets",
                column: "assigned_to_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_content_reports__users_resolved_by_id",
                table: "content_reports");

            migrationBuilder.DropForeignKey(
                name: "f_k_support_tickets__users_assigned_to_id",
                table: "support_tickets");

            migrationBuilder.DropIndex(
                name: "i_x_content_reports_resolved_by_id",
                table: "content_reports");

            migrationBuilder.DropColumn(
                name: "follower_count",
                table: "users");

            migrationBuilder.DropColumn(
                name: "following_count",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_suspended",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_active_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "profile_image",
                table: "users");

            migrationBuilder.DropColumn(
                name: "suspended_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "suspended_until",
                table: "users");

            migrationBuilder.DropColumn(
                name: "suspension_reason",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "description",
                table: "user_activities");

            migrationBuilder.DropColumn(
                name: "favorite_count",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "prompt",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "view_count",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "device_name",
                table: "fcm_tokens");

            migrationBuilder.DropColumn(
                name: "last_active_at",
                table: "fcm_tokens");

            migrationBuilder.RenameColumn(
                name: "metadata",
                table: "user_activities",
                newName: "activity_data");

            migrationBuilder.RenameColumn(
                name: "assigned_to_id",
                table: "support_tickets",
                newName: "assigned_to");

            migrationBuilder.RenameIndex(
                name: "i_x_support_tickets_assigned_to_id",
                table: "support_tickets",
                newName: "i_x_support_tickets_assigned_to");

            migrationBuilder.RenameColumn(
                name: "resolved_by_id",
                table: "content_reports",
                newName: "reviewed_by");

            migrationBuilder.RenameColumn(
                name: "resolved_at",
                table: "content_reports",
                newName: "reviewed_at");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "content_reports",
                newName: "additional_info");

            migrationBuilder.AlterColumn<string>(
                name: "target_id",
                table: "user_activities",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "support_tickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_used_at",
                table: "fcm_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "reason",
                table: "content_reports",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "content_id",
                table: "content_reports",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "f_k_support_tickets__users_assigned_to",
                table: "support_tickets",
                column: "assigned_to",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
