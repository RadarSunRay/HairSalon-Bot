using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admins",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admins", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "barbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    special = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_barbers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramUserName = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    SelectedService = table.Column<string>(type: "text", nullable: false),
                    SelectedBarberId = table.Column<int>(type: "integer", nullable: true),
                    currentState = table.Column<string>(type: "text", nullable: false),
                    SelectedTime = table.Column<string>(type: "text", nullable: false),
                    SelectedDay = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_barbers_SelectedBarberId",
                        column: x => x.SelectedBarberId,
                        principalTable: "barbers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    BarberId = table.Column<int>(type: "integer", nullable: false),
                    BookTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BookDay = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_books", x => x.Id);
                    table.ForeignKey(
                        name: "FK_books_barbers_BarberId",
                        column: x => x.BarberId,
                        principalTable: "barbers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_books_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "admins",
                columns: new[] { "id", "name", "password" },
                values: new object[] { 1, "admin", "1234" });

            migrationBuilder.InsertData(
                table: "barbers",
                columns: new[] { "Id", "Name", "special" },
                values: new object[,]
                {
                    { 1, "Светлана", "✂️ Мужская стрижка" },
                    { 2, "Анастасия", "🎨 Окрашивание" },
                    { 3, "Алина", "💇‍♀️ Женская стрижка" },
                    { 4, "Людмила", "💆‍♂️ Уход за волосами" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_books_BarberId",
                table: "books",
                column: "BarberId");

            migrationBuilder.CreateIndex(
                name: "IX_books_UserId",
                table: "books",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_SelectedBarberId",
                table: "users",
                column: "SelectedBarberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admins");

            migrationBuilder.DropTable(
                name: "books");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "barbers");
        }
    }
}
