using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityToTransactionOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "TransactionOperations",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityId",
                table: "TransactionOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOperations_SecurityId",
                table: "TransactionOperations",
                column: "SecurityId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionOperations_Securities_SecurityId",
                table: "TransactionOperations",
                column: "SecurityId",
                principalTable: "Securities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionOperations_Securities_SecurityId",
                table: "TransactionOperations");

            migrationBuilder.DropIndex(
                name: "IX_TransactionOperations_SecurityId",
                table: "TransactionOperations");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "TransactionOperations");

            migrationBuilder.DropColumn(
                name: "SecurityId",
                table: "TransactionOperations");
        }
    }
}
