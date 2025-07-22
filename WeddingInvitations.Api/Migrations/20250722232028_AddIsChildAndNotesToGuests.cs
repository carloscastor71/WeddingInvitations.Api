using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingInvitations.Api.Migrations
{
    public partial class AddIsChildAndNotesToGuests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsChild",
                table: "Guests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Guests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsChild",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Guests");
        }
    }
}
