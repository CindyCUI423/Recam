using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recam.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Unique_Hero_Media_Asset_Constraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_ListingCaseId",
                table: "MediaAssets");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ListingCaseId_IsHero",
                table: "MediaAssets",
                columns: new[] { "ListingCaseId", "IsHero" },
                unique: true,
                filter: "[IsHero] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_ListingCaseId_IsHero",
                table: "MediaAssets");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ListingCaseId",
                table: "MediaAssets",
                column: "ListingCaseId");
        }
    }
}
