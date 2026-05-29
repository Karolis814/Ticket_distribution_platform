using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTypeRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sold",
                table: "TicketTypes");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TicketTypes",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TicketTypes");

            migrationBuilder.AddColumn<int>(
                name: "Sold",
                table: "TicketTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
