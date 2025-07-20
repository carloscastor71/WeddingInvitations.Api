using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingInvitations.Api.Migrations
{
    public partial class AddEventDetailsAndTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CivilAddress",
                table: "Families",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CivilDateTime",
                table: "Families",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CivilVenue",
                table: "Families",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "FormCompleted",
                table: "Families",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FormCompletedDate",
                table: "Families",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InvitationViewed",
                table: "Families",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSent",
                table: "Families",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceptionAddress",
                table: "Families",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceptionDateTime",
                table: "Families",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ReceptionVenue",
                table: "Families",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReligiousAddress",
                table: "Families",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReligiousDateTime",
                table: "Families",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ReligiousVenue",
                table: "Families",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReminderCount",
                table: "Families",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponseDeadline",
                table: "Families",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ViewedDate",
                table: "Families",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CivilAddress",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "CivilDateTime",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "CivilVenue",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "FormCompleted",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "FormCompletedDate",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "InvitationViewed",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "LastReminderSent",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "ReceptionAddress",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "ReceptionDateTime",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "ReceptionVenue",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "ReligiousAddress",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "ReligiousDateTime",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "ReligiousVenue",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "ReminderCount",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "ResponseDeadline",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "ViewedDate",
                table: "Families");
        }
    }
}
