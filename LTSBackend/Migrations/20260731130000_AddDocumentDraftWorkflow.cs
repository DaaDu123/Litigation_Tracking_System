using LTSBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTSBackend.Migrations
{
    // NOTE: hand-written (no dotnet/EF CLI available in the sandbox this was
    // authored in) to add the three new Document columns backing the SRS
    // draft-approval workflow ("All uploaded work remains in Draft until
    // approved by Partner or Firm Admin" - Intern/Paralegal role). The
    // [DbContext]/[Migration] attributes below are what let EF discover/
    // order this migration for `dotnet ef database update` without a
    // separate Designer.cs; AppDbContextModelSnapshot.cs has already been
    // updated to match so a future `dotnet ef migrations add` diffs cleanly
    // against it. If you'd rather have EF scaffold this (and its matching
    // Designer.cs) itself, delete this file, revert the model-snapshot
    // edit, and run `dotnet ef migrations add AddDocumentDraftWorkflow`
    // instead - it will produce an equivalent migration from the
    // Document.cs model changes.
    [DbContext(typeof(AppDbContext))]
    [Migration("20260731130000_AddDocumentDraftWorkflow")]
    public partial class AddDocumentDraftWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "Documents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedBy",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "Documents",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "Documents");
        }
    }
}
