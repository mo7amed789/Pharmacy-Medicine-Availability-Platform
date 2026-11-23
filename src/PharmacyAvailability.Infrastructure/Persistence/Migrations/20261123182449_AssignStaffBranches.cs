using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyAvailability.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssignStaffBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedBranchId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_AssignedBranchId",
                table: "Users",
                column: "AssignedBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_PharmacyBranches_AssignedBranchId",
                table: "Users",
                column: "AssignedBranchId",
                principalTable: "PharmacyBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_PharmacyBranches_AssignedBranchId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_AssignedBranchId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedBranchId",
                table: "Users");
        }
    }
}
