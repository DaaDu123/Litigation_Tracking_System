using LTSBackend.Models.Audit;
using LTSBackend.Models.Cases;
using LTSBackend.Models.Masters;
using LTSBackend.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Data;

/// <summary>
/// ✅ AppDbContext: Main Database Context
/// 
/// Tamam tables ka configuration EF Core ke through
/// Isme sab DbSets, relationships, aur constraints define hain
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // ================================================================
    // ✅ SECURITY MODELS (User, Role, Permission)
    // ================================================================
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<UserOtp> UserOtps { get; set; } = null!;
    public DbSet<LoginHistory> LoginHistories { get; set; } = null!;

    // ================================================================
    // ✅ AUDIT MODELS
    // ================================================================
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    // ================================================================
    // ✅ MASTER TABLES (Court, Category, Status, Stage, etc.)
    // ================================================================
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Court> Courts { get; set; } = null!;
    public DbSet<CaseCategory> CaseCategories { get; set; } = null!;
    public DbSet<CaseStatus> CaseStatuses { get; set; } = null!;
    public DbSet<CaseStage> CaseStages { get; set; } = null!;
    public DbSet<DocumentType> DocumentTypes { get; set; } = null!;

    // ================================================================
    // ✅ CORE CASE MANAGEMENT (Cases, Parties, Assignments)
    // ================================================================
    public DbSet<Case> Cases { get; set; } = null!;
    public DbSet<CaseParty> CaseParties { get; set; } = null!;
    public DbSet<CaseAssignment> CaseAssignments { get; set; } = null!;
    public DbSet<CaseStatusHistory> CaseStatusHistories { get; set; } = null!;
    public DbSet<CaseMilestone> CaseMilestones { get; set; } = null!;

    // ================================================================
    // ✅ HEARINGS & DEADLINES
    // ================================================================
    public DbSet<Hearing> Hearings { get; set; } = null!;
    public DbSet<HearingAttendance> HearingAttendances { get; set; } = null!;
    public DbSet<Deadline> Deadlines { get; set; } = null!;

    // ================================================================
    // ✅ DOCUMENTS & NOTES (NEW - Document Management)
    // ================================================================
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentPermission> DocumentPermissions { get; set; } = null!;
    public DbSet<CaseNote> CaseNotes { get; set; } = null!;

    // ================================================================
    // ✅ NOTIFICATIONS
    // ================================================================
    public DbSet<Notification> Notifications { get; set; } = null!;

    /// <summary>
    /// ✅ OnModelCreating: Tamam entities ka configuration
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ================================================================
        // ✅ USER ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserID);
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.FullName).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            // Relationships
            entity.HasMany(e => e.RefreshTokens).WithOne(r => r.User).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.UserOtps).WithOne(o => o.User).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.LoginHistories).WithOne(l => l.User).OnDelete(DeleteBehavior.Cascade);
        });

        // ================================================================
        // ✅ ROLE ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleID);
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.RoleName).IsUnique();
            entity.HasMany(e => e.RolePermissions).WithOne(rp => rp.Role).OnDelete(DeleteBehavior.Cascade);
        });

        // ================================================================
        // ✅ PERMISSION ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionID);
            entity.Property(e => e.PermissionName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.PermissionName).IsUnique();
        });

        // ================================================================
        // ✅ ROLEPERMISSION ENTITY CONFIGURATION (join table)
        // ================================================================
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(x => new { x.RoleID, x.PermissionID });
            // FIX: made explicit — join-table rows should always die with
            // either parent, never silently rely on convention defaults.
            entity.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionID).OnDelete(DeleteBehavior.Cascade);
        });

        // ================================================================
        // ✅ REFRESHTOKEN ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenID);
            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.ExpiryDate).IsRequired();
            entity.HasOne(e => e.User).WithMany(u => u.RefreshTokens).HasForeignKey(e => e.UserID).OnDelete(DeleteBehavior.Cascade);
        });

        // ================================================================
        // ✅ USEROTP ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<UserOtp>(entity =>
        {
            entity.HasKey(e => e.OtpID);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.Property(e => e.OtpCode).IsRequired().HasMaxLength(6);
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.HasIndex(e => new { e.Email, e.OtpCode });
        });

        // ================================================================
        // ✅ LOGINHISTORY ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<LoginHistory>(entity =>
        {
            entity.HasKey(e => e.LoginID);
            entity.Property(e => e.UserID).IsRequired();
            entity.Property(e => e.LoginTime).IsRequired();
            entity.HasOne(e => e.User).WithMany(u => u.LoginHistories).HasForeignKey(e => e.UserID).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserID, e.LoginTime }).IsDescending(false, true);
        });

        // ================================================================
        // ✅ AUDITLOG ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogID);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.HasIndex(e => e.Timestamp).IsDescending();
            entity.HasIndex(e => e.UserID);
            // NOTE: no HasOne(e => e.User) configured here — EF Core is
            // relying on convention to discover this relationship via the
            // UserID/User navigation pair. It works (confirmed by
            // GetAuditLogsHandler's .Include(x => x.User) usage elsewhere),
            // but for full explicitness/auditability, consider adding it
            // here too once the AuditLog model's User navigation property
            // name is confirmed — with DeleteBehavior.Restrict, so deleting
            // a user can never silently erase their audit trail.
        });

        // ================================================================
        // ✅ CASE ENTITY CONFIGURATION (Main Case Table)
        // ================================================================
        modelBuilder.Entity<Case>(entity =>
        {
            entity.HasKey(e => e.CaseID);
            entity.Property(e => e.InternalReferenceNo).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.InternalReferenceNo).IsUnique();
            entity.Property(e => e.CaseNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CaseTitle).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Priority).IsRequired().HasMaxLength(20);
            entity.Property(e => e.SubjectMatter).IsRequired().HasMaxLength(255);

            // ================================================================
            // Foreign Keys — every one now has an EXPLICIT OnDelete.
            //
            // FIX: Category / Status / Stage previously had none specified.
            // Since these FKs are required, EF Core's default is Cascade —
            // meaning deleting a single CaseCategory / CaseStatus / CaseStage
            // master/lookup record would silently cascade-delete every Case
            // using it, which would then cascade further into Hearings,
            // Documents, Deadlines, CaseNotes, etc. Master/lookup data must
            // never be able to wipe out live transactional case data.
            // Matched to the same NoAction treatment already correctly used
            // for Court.
            // ================================================================
            entity.HasOne(e => e.Court).WithMany().HasForeignKey(e => e.CourtID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.StatusID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Stage).WithMany().HasForeignKey(e => e.StageID).OnDelete(DeleteBehavior.NoAction);

            // FIX: made explicit. ResponsibleDepartmentID is an optional FK,
            // so if a Department is deleted, cases just lose the department
            // link rather than being deleted themselves.
            entity.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.ResponsibleDepartmentID).OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.LegalOfficer).WithMany().HasForeignKey(e => e.CurrentLegalOfficerID).OnDelete(DeleteBehavior.Restrict);

            // ================================================================
            // FIX (cleanup): the CaseParties / Hearings / Deadlines /
            // CaseAssignments relationships were previously configured HERE
            // as well as, redundantly, a second time from each child
            // entity's own block below (with the FK spelled out). EF Core
            // merges duplicate configs of the same relationship without
            // error, but keeping two separate places that must always agree
            // is a maintenance risk — if one were edited without the other,
            // the mismatch would fail silently. Each relationship now has
            // exactly one source of truth: the child entity's own block,
            // where the FK is explicit. Nothing removed functionally —
            // Case still cascades to all of these exactly as before.
            // ================================================================
        });

        // ================================================================
        // ✅ CASE PARTIES ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<CaseParty>(entity =>
        {
            entity.HasKey(e => e.PartyID);
            entity.Property(e => e.PartyType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PartyName).IsRequired().HasMaxLength(255);
            entity.HasOne(e => e.Case).WithMany(c => c.CaseParties).HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
        });

        // ================================================================
        // ✅ CASE ASSIGNMENTS ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<CaseAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentID);
            entity.Property(e => e.AssignmentType).IsRequired().HasMaxLength(30);
            entity.HasOne(e => e.Case).WithMany(c => c.CaseAssignments).HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserID).OnDelete(DeleteBehavior.Restrict);
        });

        // ================================================================
        // ✅ CASE STATUS HISTORY ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<CaseStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryID);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
        });

        // ================================================================
        // ✅ CASE MILESTONES ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<CaseMilestone>(entity =>
        {
            entity.HasKey(e => e.MilestoneID);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
        });

        // ================================================================
        // ✅ HEARINGS ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<Hearing>(entity =>
        {
            entity.HasKey(e => e.HearingID);
            entity.HasOne(e => e.Case).WithMany(c => c.Hearings).HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);

            // FIX: made explicit. In practice, deleting a Court is already
            // blocked whenever any Case references it (NoAction above), so
            // this cascade to Hearings only matters for any edge case where
            // a hearing's court doesn't match its case's own court.
            entity.HasOne(e => e.Court).WithMany().HasForeignKey(e => e.CourtID).OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.HearingAttendances).WithOne(ha => ha.Hearing).OnDelete(DeleteBehavior.Cascade);
        });

        // ================================================================
        // ✅ HEARING ATTENDANCE ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<HearingAttendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceID);
            entity.HasOne(e => e.Hearing).WithMany(h => h.HearingAttendances).HasForeignKey(e => e.HearingID).OnDelete(DeleteBehavior.Cascade);

            // FIX: was unset -> defaulted to Cascade (since UserID is
            // required), which would silently delete historical attendance
            // records when a user account is removed. Restrict preserves
            // the record, consistent with CaseAssignment.User below.
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserID).OnDelete(DeleteBehavior.Restrict);
        });

        // ================================================================
        // ✅ DEADLINES ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<Deadline>(entity =>
        {
            entity.HasKey(e => e.DeadlineID);
            entity.HasOne(e => e.Case).WithMany(c => c.Deadlines).HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
        });

        // ================================================================
        // ✅ DOCUMENTS ENTITY CONFIGURATION (NEW)
        // ================================================================
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocumentID);
            // Foreign Keys
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);

            // FIX: was unset -> defaulted to Cascade (since DocumentTypeID
            // is required), meaning deleting a master DocumentType record
            // (e.g. "Affidavit") would cascade-delete every uploaded
            // document of that type. Master/lookup data must not be able
            // to delete real uploaded files' database records.
            entity.HasOne(e => e.DocumentType).WithMany().HasForeignKey(e => e.DocumentTypeID).OnDelete(DeleteBehavior.NoAction);

            // Relationships
            entity.HasMany(e => e.DocumentPermissions).WithOne(dp => dp.Document).OnDelete(DeleteBehavior.Cascade);
        });

        // ================================================================
        // ✅ DOCUMENT PERMISSIONS ENTITY CONFIGURATION (NEW)
        // ================================================================
        modelBuilder.Entity<DocumentPermission>(entity =>
        {
            entity.HasKey(e => e.PermissionID);
            entity.HasOne(e => e.Document).WithMany(d => d.DocumentPermissions).HasForeignKey(e => e.DocumentID).OnDelete(DeleteBehavior.Cascade);

            // FIX: made explicit. A permission grant tied to a Role that no
            // longer exists is meaningless, so Cascade here is intentional
            // (not a leftover convention default).
            entity.HasOne(e => e.Role).WithMany().HasForeignKey(e => e.RoleID).OnDelete(DeleteBehavior.Cascade);
        });

        // ================================================================
        // ✅ CASE NOTES ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<CaseNote>(entity =>
        {
            entity.HasKey(e => e.NoteID);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);

            // FIX: was unset -> defaulted to Cascade (since UserID is
            // required), which would silently wipe out a user's case notes
            // (legal record-keeping data) when their account is deleted.
            // Restrict preserves note history, consistent with
            // CaseAssignment.User / HearingAttendance.User above.
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserID).OnDelete(DeleteBehavior.Restrict);
        });

        // ================================================================
        // ✅ NOTIFICATIONS ENTITY CONFIGURATION
        //    NOTE: SetNull requires Notification.CaseID to be a nullable
        //    FK (e.g. long?) on the model. If it's currently a required
        //    (non-nullable) long, EF Core will throw at startup:
        //    "the property cannot be nullable... cannot use
        //    DeleteBehavior.SetNull". Please confirm Notification.CaseID
        //    is declared as `long?`.
        // ================================================================
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationID);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.SetNull);
        });
    }
}