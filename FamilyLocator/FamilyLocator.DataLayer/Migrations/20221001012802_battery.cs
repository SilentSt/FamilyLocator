using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyLocator.DataLayer.Migrations
{
    public partial class battery : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Battery",
                table: "XUCoordinates",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Battery",
                table: "XUCoordinates");
        }
    }
}
