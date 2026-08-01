using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LTSBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFirmIdToCourtsAndDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FirmID",
                table: "Departments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirmID",
                table: "Courts",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 1,
                column: "FirmID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 2,
                column: "FirmID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 3,
                column: "FirmID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 4,
                column: "FirmID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 5,
                column: "FirmID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 6,
                column: "FirmID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 7,
                column: "FirmID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 8,
                column: "FirmID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "DepartmentID",
                keyValue: 1,
                column: "FirmID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "DepartmentID",
                keyValue: 2,
                column: "FirmID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "DepartmentID",
                keyValue: 3,
                column: "FirmID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "DepartmentID",
                keyValue: 4,
                column: "FirmID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "DepartmentID",
                keyValue: 5,
                column: "FirmID",
                value: null);

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionID", "Description", "PermissionName" },
                values: new object[,]
                {
                    { 206, "View this firm's own login history", "ViewLoginHistory" },
                    { 207, "Delete/cleanup this firm's own login history records", "DeleteLoginHistory" }
                });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 17,
                columns: new[] { "PermissionID", "RoleID" },
                values: new object[] { 206, 2 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 18,
                column: "PermissionID",
                value: 202);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 19,
                column: "PermissionID",
                value: 203);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 20,
                column: "PermissionID",
                value: 304);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 21,
                column: "PermissionID",
                value: 205);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 22,
                column: "PermissionID",
                value: 301);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 23,
                column: "PermissionID",
                value: 302);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 24,
                column: "PermissionID",
                value: 303);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 25,
                column: "PermissionID",
                value: 305);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 26,
                column: "PermissionID",
                value: 306);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 27,
                column: "PermissionID",
                value: 402);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 28,
                column: "PermissionID",
                value: 307);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 29,
                columns: new[] { "PermissionID", "RoleID" },
                values: new object[] { 308, 3 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 30,
                column: "PermissionID",
                value: 401);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 31,
                column: "PermissionID",
                value: 402);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 32,
                column: "PermissionID",
                value: 403);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 33,
                column: "PermissionID",
                value: 404);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 34,
                column: "PermissionID",
                value: 405);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 35,
                columns: new[] { "PermissionID", "RoleID" },
                values: new object[] { 406, 4 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 36,
                column: "PermissionID",
                value: 501);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 37,
                column: "PermissionID",
                value: 502);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 38,
                columns: new[] { "PermissionID", "RoleID" },
                values: new object[] { 505, 5 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 39,
                column: "PermissionID",
                value: 601);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 40,
                column: "PermissionID",
                value: 602);

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RolePermissionID", "PermissionID", "RoleID" },
                values: new object[] { 41, 603, 6 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "RoleID", "SecurityStamp" },
                values: new object[] { 1, "SEED-STAMP-USER-0001" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 2,
                columns: new[] { "RoleID", "SecurityStamp" },
                values: new object[] { 2, "SEED-STAMP-USER-0002" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 3,
                columns: new[] { "RoleID", "SecurityStamp" },
                values: new object[] { 3, "SEED-STAMP-USER-0003" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 4,
                columns: new[] { "RoleID", "SecurityStamp" },
                values: new object[] { 4, "SEED-STAMP-USER-0004" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 5,
                columns: new[] { "RoleID", "SecurityStamp" },
                values: new object[] { 5, "SEED-STAMP-USER-0005" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 6,
                columns: new[] { "RoleID", "SecurityStamp" },
                values: new object[] { 6, "SEED-STAMP-USER-0006" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_FirmID",
                table: "Departments",
                column: "FirmID");

            migrationBuilder.CreateIndex(
                name: "IX_Courts_FirmID",
                table: "Courts",
                column: "FirmID");

            migrationBuilder.AddForeignKey(
                name: "FK_Courts_Firms_FirmID",
                table: "Courts",
                column: "FirmID",
                principalTable: "Firms",
                principalColumn: "FirmID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Firms_FirmID",
                table: "Departments",
                column: "FirmID",
                principalTable: "Firms",
                principalColumn: "FirmID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courts_Firms_FirmID",
                table: "Courts");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Firms_FirmID",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_FirmID",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Courts_FirmID",
                table: "Courts");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionID",
                keyValue: 206);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionID",
                keyValue: 207);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 41);

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FirmID",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "FirmID",
                table: "Courts");

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 17,
                columns: new[] { "PermissionID", "RoleID" },
                values: new object[] { 202, 3 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 18,
                column: "PermissionID",
                value: 203);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 19,
                column: "PermissionID",
                value: 304);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 20,
                column: "PermissionID",
                value: 205);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 21,
                column: "PermissionID",
                value: 301);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 22,
                column: "PermissionID",
                value: 302);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 23,
                column: "PermissionID",
                value: 303);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 24,
                column: "PermissionID",
                value: 305);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 25,
                column: "PermissionID",
                value: 306);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 26,
                column: "PermissionID",
                value: 402);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 27,
                column: "PermissionID",
                value: 307);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 28,
                column: "PermissionID",
                value: 308);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 29,
                columns: new[] { "PermissionID", "RoleID" },
                values: new object[] { 401, 4 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 30,
                column: "PermissionID",
                value: 402);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 31,
                column: "PermissionID",
                value: 403);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 32,
                column: "PermissionID",
                value: 404);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 33,
                column: "PermissionID",
                value: 405);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 34,
                column: "PermissionID",
                value: 406);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 35,
                columns: new[] { "PermissionID", "RoleID" },
                values: new object[] { 501, 5 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 36,
                column: "PermissionID",
                value: 502);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 37,
                column: "PermissionID",
                value: 505);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 38,
                columns: new[] { "PermissionID", "RoleID" },
                values: new object[] { 601, 6 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 39,
                column: "PermissionID",
                value: 602);

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "RolePermissionID",
                keyValue: 40,
                column: "PermissionID",
                value: 603);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                column: "RoleID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 2,
                column: "RoleID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 3,
                column: "RoleID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 4,
                column: "RoleID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 5,
                column: "RoleID",
                value: null);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 6,
                column: "RoleID",
                value: null);
        }
    }
}
