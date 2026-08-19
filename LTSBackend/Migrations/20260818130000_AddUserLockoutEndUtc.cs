using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTSBackend.Migrations
{
    /// <inheritdoc />
    // Adds User.LockoutEndUtc - see LoginHandler for the time-based
    // account-lockout logic this backs (MaxFailedAttempts wrong passwords
    // -> locked until this timestamp, then automatically fresh again).
    public partial class AddUserLockoutEndUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEndUtc",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockoutEndUtc",
                table: "Users");
        }
    }
}
