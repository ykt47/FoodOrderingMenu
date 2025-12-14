using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrderingMenu.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryIconField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "MenuCategories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon",
                table: "MenuCategories");
        }
    }
}
