using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTSBackend.Migrations
{
    /// <inheritdoc />
    // Adds the "request to become a Firm Admin" workflow: a public,
    // unauthenticated submission goes into this table as "Pending" and a
    // Super Admin later Approves (creates the real Firm + FirmAdmin User)
    // or Rejects it. See Features/FirmAdminRequests for the full flow.
    public partial class AddFirmAdminRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FirmAdminRequests",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirmName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FirmCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AdminFullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AdminEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AdminPasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AdminPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedFirmID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirmAdminRequests", x => x.RequestID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FirmAdminRequests_FirmCode",
                table: "FirmAdminRequests",
                column: "FirmCode");

            migrationBuilder.CreateIndex(
                name: "IX_FirmAdminRequests_AdminEmail",
                table: "FirmAdminRequests",
                column: "AdminEmail");

            migrationBuilder.CreateIndex(
                name: "IX_FirmAdminRequests_Status",
                table: "FirmAdminRequests",
                column: "Status");

            migrationBuilder.InsertData(
                table: "NotificationTypes",
                columns: new[] { "NotificationTypeID", "TypeName", "Description", "IsEmail", "IsSMS", "IsInApp", "IsActive" },
                values: new object[] { 6, "FirmAdminRequest", "Sent to every Super Admin when someone requests to become a Firm Admin", true, false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NotificationTypes",
                keyColumn: "NotificationTypeID",
                keyValue: 6);

            migrationBuilder.DropTable(
                name: "FirmAdminRequests");
        }
    }
}
