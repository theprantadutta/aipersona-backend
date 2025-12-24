using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiPersona.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonaGreeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "persona_greetings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    greeting_text = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    tokens_used = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_persona_greetings", x => x.id);
                    table.ForeignKey(
                        name: "f_k_persona_greetings__users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_persona_greetings_personas_persona_id",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_persona_greetings_persona_id",
                table: "persona_greetings",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "i_x_persona_greetings_user_id",
                table: "persona_greetings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_persona_greetings_user_id_persona_id",
                table: "persona_greetings",
                columns: new[] { "user_id", "persona_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "persona_greetings");
        }
    }
}
