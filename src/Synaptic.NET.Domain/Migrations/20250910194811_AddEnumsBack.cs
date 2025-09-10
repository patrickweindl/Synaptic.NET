using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptic.NET.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddEnumsBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IdentityRoleInteger",
                table: "Users",
                newName: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Users",
                newName: "IdentityRoleInteger");
        }
    }
}
