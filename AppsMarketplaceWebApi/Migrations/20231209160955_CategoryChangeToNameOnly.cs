using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppsMarketplaceWebApi.Migrations
{
    /// <inheritdoc />
    public partial class CategoryChangeToNameOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AppCategories",
                table: "AppCategories");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "AppCategories");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "AppCategories",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppCategories",
                table: "AppCategories",
                column: "CategoryName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AppCategories",
                table: "AppCategories");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "AppCategories",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "AppCategories",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppCategories",
                table: "AppCategories",
                column: "CategoryId");
        }
    }
}
