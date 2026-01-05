using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseLineItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the Amount column from ExpenseItems as it's now a computed property
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "ExpenseItems");

            // Create the ExpenseLineItems table
            migrationBuilder.CreateTable(
                name: "ExpenseLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseLineItems_ExpenseItems_ExpenseItemId",
                        column: x => x.ExpenseItemId,
                        principalTable: "ExpenseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create index for foreign key
            migrationBuilder.CreateIndex(
                name: "IX_ExpenseLineItems_ExpenseItemId",
                table: "ExpenseLineItems",
                column: "ExpenseItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the ExpenseLineItems table (index is dropped automatically with table)
            migrationBuilder.DropTable(
                name: "ExpenseLineItems");

            // Re-add the Amount column to ExpenseItems
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "ExpenseItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
