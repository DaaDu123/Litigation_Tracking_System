using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTSBackend.Migrations
{
    public partial class FixRolePermissionsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear whatever is currently in the table (old/drifted rows,
            // regardless of their RolePermissionID values) before
            // re-inserting the current correct set with fixed, known IDs.
            migrationBuilder.Sql("DELETE FROM [RolePermissions];");

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RolePermissionID", "PermissionID", "RoleID" },
                values: new object[,]
                {
                    // SuperAdmin (RoleID 1) - platform-owner set ONLY.
                    { 1, 101, 1 },  // ManageFirms
                    { 2, 102, 1 },  // ViewSystemAuditLogs
                    { 3, 103, 1 },  // ManageDataMigration
                    { 4, 104, 1 },  // ManageSystemUsers

                    // FirmAdmin (RoleID 2)
                    { 5, 201, 2 },  // ManageFirmUsers
                    { 6, 202, 2 },  // ViewFirmCaseDirectory
                    { 7, 203, 2 },  // AssignLawyersToCases
                    { 8, 204, 2 },  // ManageFirmSettings
                    { 9, 205, 2 },  // DeleteCases
                    { 10, 301, 2 }, // ViewFirmCases
                    { 11, 302, 2 }, // CreateCases
                    { 12, 303, 2 }, // UpdateCases
                    { 13, 305, 2 }, // ViewAllDocuments
                    { 14, 306, 2 }, // DownloadDocuments
                    { 15, 402, 2 }, // UploadDocuments
                    { 16, 308, 2 }, // ViewFirmAnalytics
                    { 17, 206, 2 }, // ViewLoginHistory
                    { 18, 208, 2 }, // ViewAuditLogs
                    { 19, 701, 2 }, // ViewDashboard

                    // Partner / Senior Lawyer (RoleID 3)
                    { 20, 202, 3 }, // ViewFirmCaseDirectory
                    { 21, 203, 3 }, // AssignLawyersToCases
                    { 22, 304, 3 }, // AssignCases
                    { 23, 205, 3 }, // DeleteCases
                    { 24, 301, 3 }, // ViewFirmCases
                    { 25, 302, 3 }, // CreateCases
                    { 26, 303, 3 }, // UpdateCases
                    { 27, 305, 3 }, // ViewAllDocuments
                    { 28, 306, 3 }, // DownloadDocuments
                    { 29, 402, 3 }, // UploadDocuments
                    { 30, 307, 3 }, // ApproveFilings
                    { 31, 308, 3 }, // ViewFirmAnalytics
                    { 32, 701, 3 }, // ViewDashboard

                    // Associate Lawyer (RoleID 4)
                    { 33, 401, 4 }, // ViewAssignedCases
                    { 34, 402, 4 }, // UploadDocuments
                    { 35, 403, 4 }, // DownloadAssignedDocuments
                    { 36, 404, 4 }, // AddCaseNotes
                    { 37, 405, 4 }, // TrackDeadlines
                    { 38, 406, 4 }, // LogBillableHours
                    { 39, 701, 4 }, // ViewDashboard

                    // Moharrir (RoleID 5)
                    { 40, 501, 5 }, // EnterCaseData
                    { 41, 502, 5 }, // UploadCaseDocuments
                    { 42, 505, 5 }, // MaintainCaseRecords
                    { 43, 701, 5 }, // ViewDashboard

                    // Intern / Paralegal (RoleID 6)
                    { 44, 601, 6 }, // ViewDocumentsReadOnly
                    { 45, 602, 6 }, // DraftDocuments
                    { 46, 603, 6 }, // PerformResearch
                    { 47, 701, 6 }  // ViewDashboard
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No meaningful "previous state" to restore (the old rows this
            // migration replaced were themselves a bug). Reverting just
            // clears the table back to empty rather than reintroducing the
            // over-permissioned SuperAdmin rows.
            migrationBuilder.Sql("DELETE FROM [RolePermissions];");
        }
    }
}
