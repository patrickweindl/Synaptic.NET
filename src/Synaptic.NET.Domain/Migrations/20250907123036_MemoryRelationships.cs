using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptic.NET.Domain.Migrations
{
    /// <inheritdoc />
    public partial class MemoryRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Memories_Groups_GroupId",
                table: "Memories");

            migrationBuilder.DropForeignKey(
                name: "FK_Memories_Users_Owner",
                table: "Memories");

            migrationBuilder.AddForeignKey(
                name: "FK_Memories_Groups_GroupId",
                table: "Memories",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Memories_Users_Owner",
                table: "Memories",
                column: "Owner",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Memories_Groups_GroupId",
                table: "Memories");

            migrationBuilder.DropForeignKey(
                name: "FK_Memories_Users_Owner",
                table: "Memories");

            migrationBuilder.AddForeignKey(
                name: "FK_Memories_Groups_GroupId",
                table: "Memories",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Memories_Users_Owner",
                table: "Memories",
                column: "Owner",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
