using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OccurredAt",
                table: "Transactions",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Portfolios",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Portfolios",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Portfolios");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Transactions",
                newName: "OccurredAt");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Portfolios",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }
    }
}
