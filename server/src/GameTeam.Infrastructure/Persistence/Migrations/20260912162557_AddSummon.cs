using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameTeam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSummon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gacha_pity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    banner_id = table.Column<string>(type: "text", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gacha_pity", x => x.id);
                    table.ForeignKey(
                        name: "FK_gacha_pity_player_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "summon_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<string>(type: "text", nullable: false),
                    banner_id = table.Column<string>(type: "text", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    seed = table.Column<long>(type: "bigint", nullable: false),
                    pity_after = table.Column<int>(type: "integer", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    pulls = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_summon_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_summon_records_player_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "player_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gacha_pity_profile_banner",
                table: "gacha_pity",
                columns: new[] { "profile_id", "banner_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_summon_records_profile_request",
                table: "summon_records",
                columns: new[] { "profile_id", "request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gacha_pity");

            migrationBuilder.DropTable(
                name: "summon_records");
        }
    }
}
