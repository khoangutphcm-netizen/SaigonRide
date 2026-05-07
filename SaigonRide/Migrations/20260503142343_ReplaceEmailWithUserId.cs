using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaigonRide.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceEmailWithUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "RentalTransactions");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "RentalTransactions");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "RentalTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RentalTransactions_UserId",
                table: "RentalTransactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RentalTransactions_Users_UserId",
                table: "RentalTransactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RentalTransactions_Users_UserId",
                table: "RentalTransactions");

            migrationBuilder.DropIndex(
                name: "IX_RentalTransactions_UserId",
                table: "RentalTransactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RentalTransactions");

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "RentalTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "RentalTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
