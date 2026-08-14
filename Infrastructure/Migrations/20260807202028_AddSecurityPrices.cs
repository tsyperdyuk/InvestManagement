using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPrice",
                table: "Securities",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousPrice",
                table: "Securities",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPrice",
                table: "Securities");

            migrationBuilder.DropColumn(
                name: "PreviousPrice",
                table: "Securities");
        }
    }
}
