using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoldCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Sold",
                table: "TicketTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sold",
                table: "TicketTypes");
        }
    }
}
