using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTSBackend.Migrations
{
    /// <inheritdoc />
    // Adds NotificationTypeID 8 ("CompleteProfile"), sent to a user
    // created via the quick-add flow (Email + Temp Password only) asking
    // them to fill in their name/phone/department after first login.
    // See CreateUserCommandHandler.
    public partial class AddCompleteProfileNotificationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "NotificationTypes",
                columns: new[] { "NotificationTypeID", "Description", "IsActive", "IsEmail", "IsInApp", "IsSMS", "TypeName" },
                values: new object[] { 8, "Sent to a newly quick-added user (Email + Temp Password only), prompting them to complete their profile", true, true, true, false, "CompleteProfile" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NotificationTypes",
                keyColumn: "NotificationTypeID",
                keyValue: 8);
        }
    }
}
