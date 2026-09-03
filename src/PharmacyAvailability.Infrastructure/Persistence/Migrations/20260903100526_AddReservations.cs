using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyAvailability.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryTransactions_Reason",
                table: "InventoryTransactions");

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PharmacyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.CheckConstraint("CK_Reservations_ExpiresAt", "[ExpiresAt] > [CreatedAt]");
                    table.CheckConstraint("CK_Reservations_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_Reservations_Status", "[Status] IN (1, 2, 3, 4, 5)");
                    table.ForeignKey(
                        name: "FK_Reservations_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Pharmacies_PharmacyId",
                        column: x => x.PharmacyId,
                        principalTable: "Pharmacies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_PharmacyBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "PharmacyBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryTransactions_Reason",
                table: "InventoryTransactions",
                sql: "[Reason] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BranchId_MedicineId_Status",
                table: "Reservations",
                columns: new[] { "BranchId", "MedicineId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CustomerId_CreatedAt",
                table: "Reservations",
                columns: new[] { "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_MedicineId",
                table: "Reservations",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PharmacyId_BranchId_Status",
                table: "Reservations",
                columns: new[] { "PharmacyId", "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Status_ExpiresAt",
                table: "Reservations",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryTransactions_Reason",
                table: "InventoryTransactions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryTransactions_Reason",
                table: "InventoryTransactions",
                sql: "[Reason] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)");
        }
    }
}
