using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyAvailability.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryPublisherReceiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryPublisherBindings",
                columns: table => new
                {
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalProductId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstallationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UnitsPerPack = table.Column<int>(type: "int", nullable: false),
                    Verified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryPublisherBindings", x => new { x.BranchId, x.ExternalProductId });
                    table.ForeignKey(
                        name: "FK_InventoryPublisherBindings_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryPublisherBindings_PharmacyBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "PharmacyBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventorySnapshotReceipts",
                columns: table => new
                {
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstallationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ItemsAccepted = table.Column<int>(type: "int", nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySnapshotReceipts", x => new { x.BranchId, x.InstallationId, x.SnapshotId });
                    table.ForeignKey(
                        name: "FK_InventorySnapshotReceipts_PharmacyBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "PharmacyBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPublisherBindings_BranchId_MedicineId",
                table: "InventoryPublisherBindings",
                columns: new[] { "BranchId", "MedicineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPublisherBindings_MedicineId",
                table: "InventoryPublisherBindings",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshotReceipts_BranchId_Sequence",
                table: "InventorySnapshotReceipts",
                columns: new[] { "BranchId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryPublisherBindings");

            migrationBuilder.DropTable(
                name: "InventorySnapshotReceipts");
        }
    }
}
