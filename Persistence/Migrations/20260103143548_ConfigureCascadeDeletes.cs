using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureCascadeDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseItems_Invoices_InvoiceId",
                table: "ExpenseItems");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseItems_Invoices_InvoiceId",
                table: "ExpenseItems",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseItems_Invoices_InvoiceId",
                table: "ExpenseItems");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseItems_Invoices_InvoiceId",
                table: "ExpenseItems",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id");
        }
    }
}
