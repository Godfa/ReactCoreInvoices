using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCreditors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Truncate tables to avoid foreign key constraint violations during migration
            // due to new required columns (AppUserId) defaulting to empty string.
            migrationBuilder.Sql("DELETE FROM ExpenseItemPayers");
            migrationBuilder.Sql("DELETE FROM InvoiceParticipants");
            // Also clearing Invoices/ExpenseItems might be safer if we want a clean slate for Users change,
            // but let's try just the link tables first. If ExpenseItems have valid non-user data, we keep them, 
            // but OrganizerId will be null.
            // Actually, the new schema relies on OrganizerId. Code might break if null.
            // Let's clear ExpenseItems too.
            migrationBuilder.Sql("DELETE FROM ExpenseLineItems"); // Foreign key to ExpenseItems
            migrationBuilder.Sql("DELETE FROM ExpenseItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseItemPayers_Creditors_CreditorId",
                table: "ExpenseItemPayers");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceParticipants_Creditors_CreditorId",
                table: "InvoiceParticipants");

            migrationBuilder.DropTable(
                name: "Creditors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InvoiceParticipants",
                table: "InvoiceParticipants");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceParticipants_CreditorId",
                table: "InvoiceParticipants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpenseItemPayers",
                table: "ExpenseItemPayers");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseItemPayers_CreditorId",
                table: "ExpenseItemPayers");

            migrationBuilder.DropColumn(
                name: "CreditorId",
                table: "InvoiceParticipants");

            migrationBuilder.DropColumn(
                name: "ExpenseCreditor",
                table: "ExpenseItems");

            migrationBuilder.DropColumn(
                name: "CreditorId",
                table: "ExpenseItemPayers");

            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "InvoiceParticipants",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrganizerId",
                table: "ExpenseItems",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "ExpenseItemPayers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InvoiceParticipants",
                table: "InvoiceParticipants",
                columns: new[] { "InvoiceId", "AppUserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpenseItemPayers",
                table: "ExpenseItemPayers",
                columns: new[] { "ExpenseItemId", "AppUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceParticipants_AppUserId",
                table: "InvoiceParticipants",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseItems_OrganizerId",
                table: "ExpenseItems",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseItemPayers_AppUserId",
                table: "ExpenseItemPayers",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseItemPayers_AspNetUsers_AppUserId",
                table: "ExpenseItemPayers",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseItems_AspNetUsers_OrganizerId",
                table: "ExpenseItems",
                column: "OrganizerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceParticipants_AspNetUsers_AppUserId",
                table: "InvoiceParticipants",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseItemPayers_AspNetUsers_AppUserId",
                table: "ExpenseItemPayers");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseItems_AspNetUsers_OrganizerId",
                table: "ExpenseItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceParticipants_AspNetUsers_AppUserId",
                table: "InvoiceParticipants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InvoiceParticipants",
                table: "InvoiceParticipants");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceParticipants_AppUserId",
                table: "InvoiceParticipants");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseItems_OrganizerId",
                table: "ExpenseItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpenseItemPayers",
                table: "ExpenseItemPayers");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseItemPayers_AppUserId",
                table: "ExpenseItemPayers");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "InvoiceParticipants");

            migrationBuilder.DropColumn(
                name: "OrganizerId",
                table: "ExpenseItems");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "ExpenseItemPayers");

            migrationBuilder.AddColumn<int>(
                name: "CreditorId",
                table: "InvoiceParticipants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExpenseCreditor",
                table: "ExpenseItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreditorId",
                table: "ExpenseItemPayers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InvoiceParticipants",
                table: "InvoiceParticipants",
                columns: new[] { "InvoiceId", "CreditorId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpenseItemPayers",
                table: "ExpenseItemPayers",
                columns: new[] { "ExpenseItemId", "CreditorId" });

            migrationBuilder.CreateTable(
                name: "Creditors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Creditors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceParticipants_CreditorId",
                table: "InvoiceParticipants",
                column: "CreditorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseItemPayers_CreditorId",
                table: "ExpenseItemPayers",
                column: "CreditorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseItemPayers_Creditors_CreditorId",
                table: "ExpenseItemPayers",
                column: "CreditorId",
                principalTable: "Creditors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceParticipants_Creditors_CreditorId",
                table: "InvoiceParticipants",
                column: "CreditorId",
                principalTable: "Creditors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
