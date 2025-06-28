using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace echoo.Migrations
{
    /// <inheritdoc />
    public partial class smallchange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "Groups",
                newName: "CreatedByUserName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedByUserName",
                table: "Groups",
                newName: "CreatedByUserId");
        }
    }
}
