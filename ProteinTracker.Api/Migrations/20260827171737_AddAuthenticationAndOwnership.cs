using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProteinTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationAndOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoodEntries_Foods_FoodId",
                table: "FoodEntries");

            migrationBuilder.DropIndex(
                name: "IX_FoodEntries_FoodId",
                table: "FoodEntries");

            // Existing single-user rows have no trustworthy owner. Remove them explicitly
            // rather than silently assigning private nutrition data to an arbitrary account.
            migrationBuilder.Sql("DELETE FROM \"FoodEntries\";");
            migrationBuilder.Sql("DELETE FROM \"DailyTargets\";");
            migrationBuilder.Sql("DELETE FROM \"Foods\";");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Foods",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "FoodEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "DailyTargets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Foods_Id_UserId",
                table: "Foods",
                columns: new[] { "Id", "UserId" });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Foods_UserId",
                table: "Foods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodEntries_FoodId_UserId",
                table: "FoodEntries",
                columns: new[] { "FoodId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_FoodEntries_UserId",
                table: "FoodEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyTargets_UserId",
                table: "DailyTargets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyTargets_Users_UserId",
                table: "DailyTargets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodEntries_Foods_FoodId_UserId",
                table: "FoodEntries",
                columns: new[] { "FoodId", "UserId" },
                principalTable: "Foods",
                principalColumns: new[] { "Id", "UserId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodEntries_Users_UserId",
                table: "FoodEntries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Foods_Users_UserId",
                table: "Foods",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyTargets_Users_UserId",
                table: "DailyTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_FoodEntries_Foods_FoodId_UserId",
                table: "FoodEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_FoodEntries_Users_UserId",
                table: "FoodEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Foods_Users_UserId",
                table: "Foods");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Foods_Id_UserId",
                table: "Foods");

            migrationBuilder.DropIndex(
                name: "IX_Foods_UserId",
                table: "Foods");

            migrationBuilder.DropIndex(
                name: "IX_FoodEntries_FoodId_UserId",
                table: "FoodEntries");

            migrationBuilder.DropIndex(
                name: "IX_FoodEntries_UserId",
                table: "FoodEntries");

            migrationBuilder.DropIndex(
                name: "IX_DailyTargets_UserId",
                table: "DailyTargets");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FoodEntries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DailyTargets");

            migrationBuilder.CreateIndex(
                name: "IX_FoodEntries_FoodId",
                table: "FoodEntries",
                column: "FoodId");

            migrationBuilder.AddForeignKey(
                name: "FK_FoodEntries_Foods_FoodId",
                table: "FoodEntries",
                column: "FoodId",
                principalTable: "Foods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
