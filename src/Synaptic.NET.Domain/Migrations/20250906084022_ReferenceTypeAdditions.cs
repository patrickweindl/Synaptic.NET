using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptic.NET.Domain.Migrations
{
    /// <inheritdoc />
    public partial class ReferenceTypeAdditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReferenceType",
                table: "Memories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceType",
                table: "Memories");
        }
    }
}
