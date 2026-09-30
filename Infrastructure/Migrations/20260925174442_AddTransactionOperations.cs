using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionOperations_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionOperations_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOperations_PortfolioId",
                table: "TransactionOperations",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOperations_TransactionId",
                table: "TransactionOperations",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOperations_UserId_CreatedAtUtc",
                table: "TransactionOperations",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionOperations");
        }
    }
}
