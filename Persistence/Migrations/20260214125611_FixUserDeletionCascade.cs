using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixUserDeletionCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseItems_AspNetUsers_OrganizerId",
                table: "ExpenseItems");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseItems_AspNetUsers_OrganizerId",
                table: "ExpenseItems",
                column: "OrganizerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseItems_AspNetUsers_OrganizerId",
                table: "ExpenseItems");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseItems_AspNetUsers_OrganizerId",
                table: "ExpenseItems",
                column: "OrganizerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
