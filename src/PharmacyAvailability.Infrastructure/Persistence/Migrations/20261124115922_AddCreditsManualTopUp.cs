using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyAvailability.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditsManualTopUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Balance = table.Column<long>(type: "bigint", nullable: false),
                    FreeQueriesRemaining = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditAccounts", x => x.Id);
                    table.CheckConstraint("CK_CreditAccounts_Balance", "[Balance] >= 0");
                    table.CheckConstraint("CK_CreditAccounts_Free", "[FreeQueriesRemaining] >= 0");
                    table.ForeignKey(
                        name: "FK_CreditAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QueryUsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    PointsCharged = table.Column<int>(type: "int", nullable: false),
                    UsedFreeQuery = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryUsageLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<long>(type: "bigint", nullable: false),
                    BalanceAfter = table.Column<long>(type: "bigint", nullable: false),
                    TransferReference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditLedgerEntries_CreditAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "CreditAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditAccounts_PublicCode",
                table: "CreditAccounts",
                column: "PublicCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditAccounts_UserId",
                table: "CreditAccounts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgerEntries_Account_Created",
                table: "CreditLedgerEntries",
                columns: new[] { "AccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditLedgerEntries_TransferReference",
                table: "CreditLedgerEntries",
                column: "TransferReference",
                unique: true,
                filter: "[TransferReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QueryUsageLogs_CreatedAt",
                table: "QueryUsageLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QueryUsageLogs_User_Created",
                table: "QueryUsageLogs",
                columns: new[] { "UserId", "CreatedAt" });

            // The ledger is append-only: the database itself refuses UPDATE and DELETE.
            migrationBuilder.Sql(@"
CREATE TRIGGER TR_CreditLedgerEntries_Immutable
ON CreditLedgerEntries
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    RAISERROR('CreditLedgerEntries is append-only.', 16, 1);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditLedgerEntries");

            migrationBuilder.DropTable(
                name: "QueryUsageLogs");

            migrationBuilder.DropTable(
                name: "CreditAccounts");
        }
    }
}
