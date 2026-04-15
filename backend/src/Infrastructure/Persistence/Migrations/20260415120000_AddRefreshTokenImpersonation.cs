using System;
using Joby.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Joby.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260415120000_AddRefreshTokenImpersonation")]
public partial class AddRefreshTokenImpersonation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ImpersonatedUserId",
            table: "RefreshTokens",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_ImpersonatedUserId",
            table: "RefreshTokens",
            column: "ImpersonatedUserId");

        migrationBuilder.AddForeignKey(
            name: "FK_RefreshTokens_Users_ImpersonatedUserId",
            table: "RefreshTokens",
            column: "ImpersonatedUserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_RefreshTokens_Users_ImpersonatedUserId",
            table: "RefreshTokens");

        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_ImpersonatedUserId",
            table: "RefreshTokens");

        migrationBuilder.DropColumn(
            name: "ImpersonatedUserId",
            table: "RefreshTokens");
    }
}
