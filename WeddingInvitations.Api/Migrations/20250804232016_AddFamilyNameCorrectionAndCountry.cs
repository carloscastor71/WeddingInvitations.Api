using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingInvitations.Api.Migrations
{
    public partial class AddFamilyNameCorrectionAndCountry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrectedFamilyName",
                table: "Families",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Families",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectedFamilyName",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Families");
        }
    }
}
