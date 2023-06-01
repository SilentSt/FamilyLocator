using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyLocator.DataLayer.Migrations
{
    public partial class confirmations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "UserConfirmations");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "UserConfirmations");

            migrationBuilder.RenameColumn(
                name: "OTP",
                table: "UserConfirmations",
                newName: "MailCode");

            migrationBuilder.RenameColumn(
                name: "ConfirmCode",
                table: "UserConfirmations",
                newName: "HashCode");

            migrationBuilder.CreateIndex(
                name: "IX_UserConfirmations_UserId",
                table: "UserConfirmations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserConfirmations_AspNetUsers_UserId",
                table: "UserConfirmations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserConfirmations_AspNetUsers_UserId",
                table: "UserConfirmations");

            migrationBuilder.DropIndex(
                name: "IX_UserConfirmations_UserId",
                table: "UserConfirmations");

            migrationBuilder.RenameColumn(
                name: "MailCode",
                table: "UserConfirmations",
                newName: "OTP");

            migrationBuilder.RenameColumn(
                name: "HashCode",
                table: "UserConfirmations",
                newName: "ConfirmCode");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "UserConfirmations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "UserConfirmations",
                type: "text",
                nullable: true);
        }
    }
}
