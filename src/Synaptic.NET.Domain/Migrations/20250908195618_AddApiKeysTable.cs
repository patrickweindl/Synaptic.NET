using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptic.NET.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeysTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiKey_Users_UserId",
                table: "ApiKey");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApiKey",
                table: "ApiKey");

            migrationBuilder.RenameTable(
                name: "ApiKey",
                newName: "ApiKeys");

            migrationBuilder.RenameIndex(
                name: "IX_ApiKey_UserId",
                table: "ApiKeys",
                newName: "IX_ApiKeys_UserId");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApiKeys",
                table: "ApiKeys",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UserId1",
                table: "ApiKeys",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Users_UserId",
                table: "ApiKeys",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Users_UserId1",
                table: "ApiKeys",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_Users_UserId",
                table: "ApiKeys");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_Users_UserId1",
                table: "ApiKeys");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApiKeys",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_UserId1",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "ApiKeys");

            migrationBuilder.RenameTable(
                name: "ApiKeys",
                newName: "ApiKey");

            migrationBuilder.RenameIndex(
                name: "IX_ApiKeys_UserId",
                table: "ApiKey",
                newName: "IX_ApiKey_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApiKey",
                table: "ApiKey",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKey_Users_UserId",
                table: "ApiKey",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
