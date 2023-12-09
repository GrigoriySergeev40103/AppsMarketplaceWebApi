using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppsMarketplaceWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AppChangeCategoryToStr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Apps");

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "Apps",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "Apps");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Apps",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
