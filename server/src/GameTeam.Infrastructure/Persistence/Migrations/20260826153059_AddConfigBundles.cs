using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameTeam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigBundles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "config_bundles",
                columns: table => new
                {
                    version = table.Column<int>(type: "integer", nullable: false),
                    config_version = table.Column<string>(type: "text", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    checksum = table.Column<string>(type: "text", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_bundles", x => x.version);
                });

            migrationBuilder.CreateTable(
                name: "config_current",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    current_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_current", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_config_bundles_config_version",
                table: "config_bundles",
                column: "config_version",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "config_bundles");

            migrationBuilder.DropTable(
                name: "config_current");
        }
    }
}
