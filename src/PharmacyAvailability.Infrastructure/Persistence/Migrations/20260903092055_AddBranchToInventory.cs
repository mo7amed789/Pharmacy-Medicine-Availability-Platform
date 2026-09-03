using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyAvailability.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchToInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PharmacyInventories_Medicine_Stock_Covering",
                table: "PharmacyInventories");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyInventories_PharmacyId_MedicineId",
                table: "PharmacyInventories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryTransactions_Reason",
                table: "InventoryTransactions");

            // Production-safe migration: if any legacy inventory items lack BranchId, link them to the pharmacy's earliest branch
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PharmacyInventories') AND name = 'BranchId')
                BEGIN
                    UPDATE pi
                    SET BranchId = (SELECT TOP 1 b.Id FROM PharmacyBranches b WHERE b.PharmacyId = pi.PharmacyId ORDER BY b.CreatedAt ASC)
                    FROM PharmacyInventories pi
                    WHERE pi.BranchId = '00000000-0000-0000-0000-000000000000'
                      AND EXISTS (SELECT 1 FROM PharmacyBranches b WHERE b.PharmacyId = pi.PharmacyId);

                    -- Only purge orphaned records where no valid branch exists for the parent pharmacy
                    DELETE ph FROM PriceHistories ph WHERE PharmacyInventoryId IN (SELECT Id FROM PharmacyInventories WHERE BranchId = '00000000-0000-0000-0000-000000000000');
                    DELETE it FROM InventoryTransactions it WHERE PharmacyInventoryId IN (SELECT Id FROM PharmacyInventories WHERE BranchId = '00000000-0000-0000-0000-000000000000');
                    DELETE FROM PharmacyInventories WHERE BranchId = '00000000-0000-0000-0000-000000000000';
                END
            ");

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "PharmacyInventories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyInventories_BranchId",
                table: "PharmacyInventories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyInventories_BranchId_MedicineId",
                table: "PharmacyInventories",
                columns: new[] { "BranchId", "MedicineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyInventories_Medicine_Stock_Covering",
                table: "PharmacyInventories",
                columns: new[] { "MedicineId", "AvailableQuantity" })
                .Annotation("SqlServer:Include", new[] { "BranchId", "PharmacyId", "Price", "UpdatedAt", "Source" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryTransactions_Reason",
                table: "InventoryTransactions",
                sql: "[Reason] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)");

            migrationBuilder.AddForeignKey(
                name: "FK_PharmacyInventories_PharmacyBranches_BranchId",
                table: "PharmacyInventories",
                column: "BranchId",
                principalTable: "PharmacyBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PharmacyInventories_PharmacyBranches_BranchId",
                table: "PharmacyInventories");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyInventories_BranchId",
                table: "PharmacyInventories");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyInventories_BranchId_MedicineId",
                table: "PharmacyInventories");

            migrationBuilder.DropIndex(
                name: "IX_PharmacyInventories_Medicine_Stock_Covering",
                table: "PharmacyInventories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryTransactions_Reason",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PharmacyInventories");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyInventories_Medicine_Stock_Covering",
                table: "PharmacyInventories",
                columns: new[] { "MedicineId", "AvailableQuantity" })
                .Annotation("SqlServer:Include", new[] { "PharmacyId", "Price", "UpdatedAt", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyInventories_PharmacyId_MedicineId",
                table: "PharmacyInventories",
                columns: new[] { "PharmacyId", "MedicineId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryTransactions_Reason",
                table: "InventoryTransactions",
                sql: "[Reason] IN (1, 2, 3, 4, 5, 6, 7)");
        }
    }
}
