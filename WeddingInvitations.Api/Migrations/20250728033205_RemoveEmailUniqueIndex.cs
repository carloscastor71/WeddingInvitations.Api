using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingInvitations.Api.Migrations
{
    public partial class RemoveEmailUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Families_Email",
                table: "Families");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Families_Email",
                table: "Families",
                column: "Email",
                unique: true);
        }
    }
}
