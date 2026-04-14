using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Joby.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetCodeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetCodeExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetCodeHash",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetCodeSentAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetCodeExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetCodeHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetCodeSentAt",
                table: "Users");
        }
    }
}
