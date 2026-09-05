using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameTeam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnedHeroes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "owned_heroes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hero_id = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    stars = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_owned_heroes", x => x.id);
                    table.ForeignKey(
                        name: "FK_owned_heroes_player_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_owned_heroes_profile_id",
                table: "owned_heroes",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_owned_heroes_profile_id_hero_id",
                table: "owned_heroes",
                columns: new[] { "profile_id", "hero_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "owned_heroes");
        }
    }
}
