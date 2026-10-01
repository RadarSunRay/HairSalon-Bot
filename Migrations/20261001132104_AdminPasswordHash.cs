using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bot.Migrations
{
    /// <inheritdoc />
    public partial class AdminPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "admins",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.RenameColumn(
                name: "password",
                table: "admins",
                newName: "PasswordHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "admins",
                newName: "password");

            migrationBuilder.InsertData(
                table: "admins",
                columns: new[] { "id", "name", "password" },
                values: new object[] { 1, "admin", "1234" });
        }
    }
}
