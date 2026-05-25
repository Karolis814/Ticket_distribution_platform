using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueTitleToPermisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Title",
                table: "Permissions",
                column: "Title",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Permissions_Title",
                table: "Permissions");
        }
    }
}
