using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenamePortfolioSecurityToPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioSecurities");

            migrationBuilder.RenameColumn(
                name: "Symbol",
                table: "Securities",
                newName: "Ticker");

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Positions_Securities_SecurityId",
                        column: x => x.SecurityId,
                        principalTable: "Securities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Positions_PortfolioId_SecurityId",
                table: "Positions",
                columns: new[] { "PortfolioId", "SecurityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_SecurityId",
                table: "Positions",
                column: "SecurityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.RenameColumn(
                name: "Ticker",
                table: "Securities",
                newName: "Symbol");

            migrationBuilder.CreateTable(
                name: "PortfolioSecurities",
                columns: table => new
                {
                    PortfolioId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioSecurities", x => new { x.PortfolioId, x.SecurityId });
                    table.ForeignKey(
                        name: "FK_PortfolioSecurities_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PortfolioSecurities_Securities_SecurityId",
                        column: x => x.SecurityId,
                        principalTable: "Securities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioSecurities_SecurityId",
                table: "PortfolioSecurities",
                column: "SecurityId");
        }
    }
}
