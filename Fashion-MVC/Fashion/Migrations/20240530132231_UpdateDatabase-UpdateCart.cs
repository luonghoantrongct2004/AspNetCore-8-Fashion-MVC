using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fashion.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabaseUpdateCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "CartDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "CartDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "CartDetails");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "CartDetails");
        }
    }
}
