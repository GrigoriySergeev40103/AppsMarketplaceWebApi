using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppsMarketplaceWebApi.Migrations
{
    /// <inheritdoc />
    public partial class ChangedToMultipleAppPics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppPicturePath",
                table: "Apps");

            migrationBuilder.CreateTable(
                name: "AppPictures",
                columns: table => new
                {
                    PictureId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AppId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Path = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPictures", x => x.PictureId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppPictures");

            migrationBuilder.AddColumn<string>(
                name: "AppPicturePath",
                table: "Apps",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
