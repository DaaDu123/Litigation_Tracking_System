using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LTSBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogAndDashboardPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BUG FIX: [HasPermission("ViewAuditLogs")] on AuditLogsController and
            // [HasPermission("ViewDashboard")] on DashboardController were used from
            // day one but neither permission was ever seeded to any role - only
            // SuperAdmin (via the old blanket "grant every permission" bypass,
            // since removed - see PermissionService) could ever reach either
            // endpoint. This adds the two missing permissions and grants them to
            // the roles that should actually have them:
            //  - ViewAuditLogs  -> FirmAdmin only (own-firm audit trail;
            //                      GetAuditLogsHandler already scopes it)
            //  - ViewDashboard  -> every role (each gets its own dashboard shape -
            //                      see GetSuperAdminDashboardHandler /
            //                      GetFirmDashboardHandler)
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionID", "Description", "PermissionName" },
                values: new object[,]
                {
                    { 208, "View this firm's own audit trail", "ViewAuditLogs" },
                    { 701, "View one's own role-scoped dashboard", "ViewDashboard" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RolePermissionID", "PermissionID", "RoleID" },
                values: new object[,]
                {
                    { 42, 208, 2 }, // FirmAdmin  -> ViewAuditLogs
                    { 43, 701, 2 }, // FirmAdmin  -> ViewDashboard
                    { 44, 701, 3 }, // Partner    -> ViewDashboard
                    { 45, 701, 4 }, // AssociateLawyer -> ViewDashboard
                    { 46, 701, 5 }, // Moharrir   -> ViewDashboard
                    { 47, 701, 6 }  // InternParalegal -> ViewDashboard
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValues: new object[] { 42, 43, 44, 45, 46, 47 });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionID",
                keyValues: new object[] { 208, 701 });
        }
    }
}
