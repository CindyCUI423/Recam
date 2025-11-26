using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recam.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Create_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agent_User_Id",
                table: "Agent");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentListingCase_Agent_AgentId",
                table: "AgentListingCase");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentListingCase_ListingCase_ListingCaseId",
                table: "AgentListingCase");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentPhotographyCompany_Agent_AgentId",
                table: "AgentPhotographyCompany");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentPhotographyCompany_PhotographyCompany_PhotographyCompanyId",
                table: "AgentPhotographyCompany");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseContact_ListingCase_ListingCaseId",
                table: "CaseContact");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaAsset_ListingCase_ListingCaseId",
                table: "MediaAsset");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaAsset_User_UserId",
                table: "MediaAsset");

            migrationBuilder.DropForeignKey(
                name: "FK_PhotographyCompany_User_Id",
                table: "PhotographyCompany");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhotographyCompany",
                table: "PhotographyCompany");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MediaAsset",
                table: "MediaAsset");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ListingCase",
                table: "ListingCase");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CaseContact",
                table: "CaseContact");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AgentPhotographyCompany",
                table: "AgentPhotographyCompany");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AgentListingCase",
                table: "AgentListingCase");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Agent",
                table: "Agent");

            migrationBuilder.RenameTable(
                name: "PhotographyCompany",
                newName: "PhotographyCompanies");

            migrationBuilder.RenameTable(
                name: "MediaAsset",
                newName: "MediaAssets");

            migrationBuilder.RenameTable(
                name: "ListingCase",
                newName: "ListingCases");

            migrationBuilder.RenameTable(
                name: "CaseContact",
                newName: "CaseContacts");

            migrationBuilder.RenameTable(
                name: "AgentPhotographyCompany",
                newName: "AgentPhotographyCompanies");

            migrationBuilder.RenameTable(
                name: "AgentListingCase",
                newName: "AgentListingCases");

            migrationBuilder.RenameTable(
                name: "Agent",
                newName: "Agents");

            migrationBuilder.RenameIndex(
                name: "IX_MediaAsset_UserId",
                table: "MediaAssets",
                newName: "IX_MediaAssets_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MediaAsset_ListingCaseId",
                table: "MediaAssets",
                newName: "IX_MediaAssets_ListingCaseId");

            migrationBuilder.RenameIndex(
                name: "IX_CaseContact_ListingCaseId",
                table: "CaseContacts",
                newName: "IX_CaseContacts_ListingCaseId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentPhotographyCompany_PhotographyCompanyId",
                table: "AgentPhotographyCompanies",
                newName: "IX_AgentPhotographyCompanies_PhotographyCompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentListingCase_ListingCaseId",
                table: "AgentListingCases",
                newName: "IX_AgentListingCases_ListingCaseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhotographyCompanies",
                table: "PhotographyCompanies",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MediaAssets",
                table: "MediaAssets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ListingCases",
                table: "ListingCases",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CaseContacts",
                table: "CaseContacts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AgentPhotographyCompanies",
                table: "AgentPhotographyCompanies",
                columns: new[] { "AgentId", "PhotographyCompanyId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AgentListingCases",
                table: "AgentListingCases",
                columns: new[] { "AgentId", "ListingCaseId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Agents",
                table: "Agents",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentListingCases_Agents_AgentId",
                table: "AgentListingCases",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentListingCases_ListingCases_ListingCaseId",
                table: "AgentListingCases",
                column: "ListingCaseId",
                principalTable: "ListingCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentPhotographyCompanies_Agents_AgentId",
                table: "AgentPhotographyCompanies",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentPhotographyCompanies_PhotographyCompanies_PhotographyCompanyId",
                table: "AgentPhotographyCompanies",
                column: "PhotographyCompanyId",
                principalTable: "PhotographyCompanies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_User_Id",
                table: "Agents",
                column: "Id",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseContacts_ListingCases_ListingCaseId",
                table: "CaseContacts",
                column: "ListingCaseId",
                principalTable: "ListingCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaAssets_ListingCases_ListingCaseId",
                table: "MediaAssets",
                column: "ListingCaseId",
                principalTable: "ListingCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaAssets_User_UserId",
                table: "MediaAssets",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotographyCompanies_User_Id",
                table: "PhotographyCompanies",
                column: "Id",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentListingCases_Agents_AgentId",
                table: "AgentListingCases");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentListingCases_ListingCases_ListingCaseId",
                table: "AgentListingCases");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentPhotographyCompanies_Agents_AgentId",
                table: "AgentPhotographyCompanies");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentPhotographyCompanies_PhotographyCompanies_PhotographyCompanyId",
                table: "AgentPhotographyCompanies");

            migrationBuilder.DropForeignKey(
                name: "FK_Agents_User_Id",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseContacts_ListingCases_ListingCaseId",
                table: "CaseContacts");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaAssets_ListingCases_ListingCaseId",
                table: "MediaAssets");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaAssets_User_UserId",
                table: "MediaAssets");

            migrationBuilder.DropForeignKey(
                name: "FK_PhotographyCompanies_User_Id",
                table: "PhotographyCompanies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhotographyCompanies",
                table: "PhotographyCompanies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MediaAssets",
                table: "MediaAssets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ListingCases",
                table: "ListingCases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CaseContacts",
                table: "CaseContacts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Agents",
                table: "Agents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AgentPhotographyCompanies",
                table: "AgentPhotographyCompanies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AgentListingCases",
                table: "AgentListingCases");

            migrationBuilder.RenameTable(
                name: "PhotographyCompanies",
                newName: "PhotographyCompany");

            migrationBuilder.RenameTable(
                name: "MediaAssets",
                newName: "MediaAsset");

            migrationBuilder.RenameTable(
                name: "ListingCases",
                newName: "ListingCase");

            migrationBuilder.RenameTable(
                name: "CaseContacts",
                newName: "CaseContact");

            migrationBuilder.RenameTable(
                name: "Agents",
                newName: "Agent");

            migrationBuilder.RenameTable(
                name: "AgentPhotographyCompanies",
                newName: "AgentPhotographyCompany");

            migrationBuilder.RenameTable(
                name: "AgentListingCases",
                newName: "AgentListingCase");

            migrationBuilder.RenameIndex(
                name: "IX_MediaAssets_UserId",
                table: "MediaAsset",
                newName: "IX_MediaAsset_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MediaAssets_ListingCaseId",
                table: "MediaAsset",
                newName: "IX_MediaAsset_ListingCaseId");

            migrationBuilder.RenameIndex(
                name: "IX_CaseContacts_ListingCaseId",
                table: "CaseContact",
                newName: "IX_CaseContact_ListingCaseId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentPhotographyCompanies_PhotographyCompanyId",
                table: "AgentPhotographyCompany",
                newName: "IX_AgentPhotographyCompany_PhotographyCompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentListingCases_ListingCaseId",
                table: "AgentListingCase",
                newName: "IX_AgentListingCase_ListingCaseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhotographyCompany",
                table: "PhotographyCompany",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MediaAsset",
                table: "MediaAsset",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ListingCase",
                table: "ListingCase",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CaseContact",
                table: "CaseContact",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Agent",
                table: "Agent",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AgentPhotographyCompany",
                table: "AgentPhotographyCompany",
                columns: new[] { "AgentId", "PhotographyCompanyId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AgentListingCase",
                table: "AgentListingCase",
                columns: new[] { "AgentId", "ListingCaseId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Agent_User_Id",
                table: "Agent",
                column: "Id",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentListingCase_Agent_AgentId",
                table: "AgentListingCase",
                column: "AgentId",
                principalTable: "Agent",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentListingCase_ListingCase_ListingCaseId",
                table: "AgentListingCase",
                column: "ListingCaseId",
                principalTable: "ListingCase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentPhotographyCompany_Agent_AgentId",
                table: "AgentPhotographyCompany",
                column: "AgentId",
                principalTable: "Agent",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentPhotographyCompany_PhotographyCompany_PhotographyCompanyId",
                table: "AgentPhotographyCompany",
                column: "PhotographyCompanyId",
                principalTable: "PhotographyCompany",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseContact_ListingCase_ListingCaseId",
                table: "CaseContact",
                column: "ListingCaseId",
                principalTable: "ListingCase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaAsset_ListingCase_ListingCaseId",
                table: "MediaAsset",
                column: "ListingCaseId",
                principalTable: "ListingCase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaAsset_User_UserId",
                table: "MediaAsset",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotographyCompany_User_Id",
                table: "PhotographyCompany",
                column: "Id",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
