using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmacyAvailability.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HashVerificationCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // OTPs are short-lived. Remove old codes rather than retain plaintext or
            // truncate hashes during rollback. Users can request a new code.
            migrationBuilder.Sql("DELETE FROM [VerificationCodes]");
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "VerificationCodes",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // OTPs are short-lived. Remove old codes rather than retain plaintext or
            // truncate hashes during rollback. Users can request a new code.
            migrationBuilder.Sql("DELETE FROM [VerificationCodes]");
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "VerificationCodes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);
        }
    }
}
