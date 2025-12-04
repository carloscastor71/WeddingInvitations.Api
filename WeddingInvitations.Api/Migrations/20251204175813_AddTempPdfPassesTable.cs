using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WeddingInvitations.Api.Migrations
{
    public partial class AddTempPdfPassesTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TempPdfPasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamilyId = table.Column<int>(type: "integer", nullable: true),
                    TableId = table.Column<int>(type: "integer", nullable: true),
                    InvitationCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PdfData = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TempPdfPasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TempPdfPasses_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TempPdfPasses_Tables_TableId",
                        column: x => x.TableId,
                        principalTable: "Tables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TempPdfPasses_ExpiresAt",
                table: "TempPdfPasses",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TempPdfPasses_FamilyId",
                table: "TempPdfPasses",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_TempPdfPasses_FamilyId_TableId",
                table: "TempPdfPasses",
                columns: new[] { "FamilyId", "TableId" });

            migrationBuilder.CreateIndex(
                name: "IX_TempPdfPasses_FileName",
                table: "TempPdfPasses",
                column: "FileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TempPdfPasses_TableId",
                table: "TempPdfPasses",
                column: "TableId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TempPdfPasses");
        }
    }
}
