using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTSBackend.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "NotificationTypes",
                columns: new[] { "NotificationTypeID", "Description", "IsActive", "IsEmail", "IsInApp", "IsSMS", "TypeName" },
                values: new object[] { 7, "Sent to a firm's Firm Admin(s) when someone requests to join that firm as Partner/Associate/Moharrir/Intern", true, true, true, false, "UserJoinRequest" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NotificationTypes",
                keyColumn: "NotificationTypeID",
                keyValue: 7);
        }
    }
}
