using Joby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Joby.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260412143000_ReminderJobAndEmail")]
public partial class ReminderJobAndEmail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "EmailSentAt",
            table: "Reminders",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "JobId",
            table: "Reminders",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Reminders_JobId",
            table: "Reminders",
            column: "JobId");

        migrationBuilder.AddForeignKey(
            name: "FK_Reminders_Jobs_JobId",
            table: "Reminders",
            column: "JobId",
            principalTable: "Jobs",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.Sql(
            """
            UPDATE "Reminders" AS r
            SET "JobId" = a."JobId"
            FROM "Applications" AS a
            WHERE r."ApplicationId" = a."Id" AND r."JobId" IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Reminders_Jobs_JobId",
            table: "Reminders");

        migrationBuilder.DropIndex(
            name: "IX_Reminders_JobId",
            table: "Reminders");

        migrationBuilder.DropColumn(
            name: "EmailSentAt",
            table: "Reminders");

        migrationBuilder.DropColumn(
            name: "JobId",
            table: "Reminders");
    }
}
