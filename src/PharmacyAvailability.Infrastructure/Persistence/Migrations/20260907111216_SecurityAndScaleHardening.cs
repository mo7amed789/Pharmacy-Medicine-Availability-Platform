using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyAvailability.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SecurityAndScaleHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerificationCodes_UserId_Type_IsUsed",
                table: "VerificationCodes");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_UserId_Type_IsUsed",
                table: "VerificationCodes",
                columns: new[] { "UserId", "Type", "IsUsed", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyBranches_Active_City_Pharmacy_Name",
                table: "PharmacyBranches",
                columns: new[] { "IsActive", "City", "PharmacyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Pharmacies_Status_City_Name",
                table: "Pharmacies",
                columns: new[] { "Status", "City", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerificationCodes_UserId_Type_IsUsed",
                table: "VerificationCodes");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyBranches_Active_City_Pharmacy_Name",
                table: "PharmacyBranches");

            migrationBuilder.DropIndex(
                name: "IX_Pharmacies_Status_City_Name",
                table: "Pharmacies");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_UserId_Type_IsUsed",
                table: "VerificationCodes",
                columns: new[] { "UserId", "Type", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash");
        }
    }
}
