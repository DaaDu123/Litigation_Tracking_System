using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTSBackend.Migrations
{
    /// <inheritdoc />
    public partial class Court_IsActive_Case_NullableFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Jurisdiction",
                table: "Courts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "CourtType",
                table: "Courts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Courts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Courts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Courts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "ResponsibleDepartmentID",
                table: "Cases",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CurrentLegalOfficerID",
                table: "Cases",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 1,
                columns: new[] { "CreatedDate", "IsActive" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true });

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 2,
                columns: new[] { "CreatedDate", "IsActive" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true });

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 3,
                columns: new[] { "CreatedDate", "IsActive" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true });

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 4,
                columns: new[] { "CreatedDate", "IsActive" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true });

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 5,
                columns: new[] { "CreatedDate", "IsActive" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true });

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 6,
                columns: new[] { "CreatedDate", "IsActive" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true });

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 7,
                columns: new[] { "CreatedDate", "IsActive" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true });

            migrationBuilder.UpdateData(
                table: "Courts",
                keyColumn: "CourtID",
                keyValue: 8,
                columns: new[] { "CreatedDate", "IsActive" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Courts");

            migrationBuilder.AlterColumn<string>(
                name: "Jurisdiction",
                table: "Courts",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CourtType",
                table: "Courts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Courts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ResponsibleDepartmentID",
                table: "Cases",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CurrentLegalOfficerID",
                table: "Cases",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
