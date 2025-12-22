using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    public partial class SyncedExpenseItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseItem_Invoices_InvoiceId",
                table: "ExpenseItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpenseItem",
                table: "ExpenseItem");

            migrationBuilder.RenameTable(
                name: "ExpenseItem",
                newName: "ExpenseItems");

            migrationBuilder.RenameIndex(
                name: "IX_ExpenseItem_InvoiceId",
                table: "ExpenseItems",
                newName: "IX_ExpenseItems_InvoiceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpenseItems",
                table: "ExpenseItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseItems_Invoices_InvoiceId",
                table: "ExpenseItems",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseItems_Invoices_InvoiceId",
                table: "ExpenseItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpenseItems",
                table: "ExpenseItems");

            migrationBuilder.RenameTable(
                name: "ExpenseItems",
                newName: "ExpenseItem");

            migrationBuilder.RenameIndex(
                name: "IX_ExpenseItems_InvoiceId",
                table: "ExpenseItem",
                newName: "IX_ExpenseItem_InvoiceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpenseItem",
                table: "ExpenseItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseItem_Invoices_InvoiceId",
                table: "ExpenseItem",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id");
        }
    }
}
