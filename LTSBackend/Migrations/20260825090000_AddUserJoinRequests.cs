using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTSBackend.Migrations
{
    /// <inheritdoc />
    // Adds the "request to join an existing firm" workflow: a public,
    // unauthenticated submission for Partner / Associate Lawyer / Moharrir /
    // Intern Paralegal goes into this table as "Pending" and the target
    // firm's FirmAdmin later Approves (creates the real, firm-scoped User)
    // or Rejects it. Mirrors AddFirmAdminRequests one tier down. See
    // Features/UserJoinRequests for the full flow.
    public partial class AddUserJoinRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserJoinRequests",
                columns: table => new
                {
                    RequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirmID = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestedRoleID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedUserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserJoinRequests", x => x.RequestID);
                    table.ForeignKey(
                        name: "FK_UserJoinRequests_Firms_FirmID",
                        column: x => x.FirmID,
                        principalTable: "Firms",
                        principalColumn: "FirmID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserJoinRequests_FirmID",
                table: "UserJoinRequests",
                column: "FirmID");

            migrationBuilder.CreateIndex(
                name: "IX_UserJoinRequests_Email",
                table: "UserJoinRequests",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_UserJoinRequests_Status",
                table: "UserJoinRequests",
                column: "Status");

            migrationBuilder.InsertData(
                table: "NotificationTypes",
                columns: new[] { "NotificationTypeID", "TypeName", "Description", "IsEmail", "IsSMS", "IsInApp", "IsActive" },
                values: new object[] { 7, "UserJoinRequest", "Sent to a firm's Firm Admin(s) when someone requests to join that firm as Partner/Associate/Moharrir/Intern", true, false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NotificationTypes",
                keyColumn: "NotificationTypeID",
                keyValue: 7);

            migrationBuilder.DropTable(
                name: "UserJoinRequests");
        }
    }
}
