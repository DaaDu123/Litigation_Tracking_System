using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTSBackend.Migrations
{
    /// <inheritdoc />
    // Part of the User Reuse / Soft-Delete / Email-Ownership fix.
    // Adds the single new column needed to distinguish "deleted but still
    // owned by the original firm" from "deleted and released for
    // reassignment to another firm" — see User.IsReleasedForReuse,
    // CreateUserCommandHandler, and the new ReleaseUserEmail command.
    public partial class AddUserEmailReuseTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReleasedForReuse",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReleasedForReuse",
                table: "Users");
        }
    }
}
