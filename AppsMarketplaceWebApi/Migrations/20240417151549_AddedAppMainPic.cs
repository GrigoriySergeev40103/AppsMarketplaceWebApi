using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppsMarketplaceWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddedAppMainPic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppMainPicPath",
                table: "Apps",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppMainPicPath",
                table: "Apps");
        }
    }
}
