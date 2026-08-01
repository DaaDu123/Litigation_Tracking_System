using System.Security.Claims;
using LTSBackend.Models.Audit;
using LTSBackend.Models.Cases;
using LTSBackend.Models.Masters;
using LTSBackend.Models.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UserRole = LTSBackend.Comman.Enum.UserRole;
using LTSBackend.Comman.Enum;

namespace LTSBackend.Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // The identity of the caller making the current request, if any. Null
    // outside of an HTTP request (migrations, seeding, background services).
    private ClaimsPrincipal? RequestUser => _httpContextAccessor?.HttpContext?.User;

    private bool IsAuthenticatedRequest => RequestUser?.Identity?.IsAuthenticated == true;

    private bool IsSuperAdminRequest => RequestUser?.FindFirstValue(ClaimTypes.Role) == RoleNames.SuperAdmin;

    // The acting user's FirmID claim, parsed from the current JWT. Null for
    // SuperAdmin (who has no firm) and for requests with no FirmID claim.
    private int? RequestFirmId
    {
        get
        {
            var value = RequestUser?.FindFirstValue("FirmID");
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    /// <summary>
    /// True when tenant scoping must be skipped entirely: there is no HTTP
    /// request in flight (migrations/seeding/background jobs), the caller
    /// is not authenticated yet (e.g. login/forgot-password looking a user
    /// up by email before any FirmID is known), or the caller is the
    /// platform-wide Super Admin, who is explicitly allowed to see every
    /// tenant (SRS "Super Admin can access every tenant. No other role can.").
    /// </summary>
    private bool BypassTenantFilter => !IsAuthenticatedRequest || IsSuperAdminRequest;

    // ================================================================
    // ✅ SECURITY MODELS (User, Role, Permission)
    // ================================================================
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Firm> Firms { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<NotificationType> NotificationTypes { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<UserOtp> UserOtps { get; set; } = null!;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
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
    // ✅ DOCUMENTS & NOTES
    // ================================================================
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentPermission> DocumentPermissions { get; set; } = null!;
    public DbSet<CaseNote> CaseNotes { get; set; } = null!;

    // ================================================================
    // ✅ NOTIFICATIONS
    // ================================================================
    public DbSet<Notification> Notifications { get; set; } = null!;

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
            entity.HasOne(e => e.Firm).WithMany(f => f.Users).HasForeignKey(e => e.FirmID).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.FirmID);
            entity.HasMany(e => e.RefreshTokens).WithOne(r => r.User).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.UserOtps).WithOne(o => o.User).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.PasswordResetTokens).WithOne(t => t.User).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.LoginHistories).WithOne(l => l.User).OnDelete(DeleteBehavior.Cascade);

            // Global filter: hide soft-deleted users, and restrict every
            // query to the caller's own firm unless bypassed (SuperAdmin /
            // background job / not-yet-authenticated login lookup).
            entity.HasQueryFilter(e => !e.IsDeleted && (BypassTenantFilter || e.FirmID == RequestFirmId));
        });

        // ================================================================
        // ✅ FIRM ENTITY CONFIGURATION (multi-tenant workspace)
        // ================================================================
        modelBuilder.Entity<Firm>(entity =>
        {
            entity.HasKey(e => e.FirmID);
            entity.Property(e => e.FirmName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.FirmCode).IsRequired().HasMaxLength(30);
            entity.HasIndex(e => e.FirmCode).IsUnique();
            entity.Property(e => e.MigrationStatus).IsRequired().HasMaxLength(30).HasDefaultValue("None");
            entity.Property(e => e.MigrationNotes).HasMaxLength(500);
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
            entity.HasKey(x => x.RolePermissionID);
            entity.HasIndex(x => new { x.RoleID, x.PermissionID }).IsUnique();
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

            // Cascading tenant filter via the owning User's FirmID. Harmless
            // during refresh-token exchange itself (that request isn't
            // authenticated via the access token, so BypassTenantFilter is
            // true), but protects any future admin-facing "list tokens" view.
            entity.HasQueryFilter(e => BypassTenantFilter || e.User.FirmID == RequestFirmId);
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
            entity.HasOne(e => e.User).WithMany(u => u.UserOtps).HasForeignKey(e => e.UserID).OnDelete(DeleteBehavior.Cascade).IsRequired(false);
            entity.HasIndex(e => new { e.Email, e.OtpCode });

            // Cascading tenant filter via the (optional) owning User's FirmID.
            // OTP flows run before authentication (registration/forgot
            // password), so BypassTenantFilter is true there and this filter
            // only matters for any future authenticated OTP management view.
            entity.HasQueryFilter(e => BypassTenantFilter || e.User == null || e.User.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ PASSWORDRESETTOKEN ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.TokenID);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.HasOne(e => e.User).WithMany(u => u.PasswordResetTokens).HasForeignKey(e => e.UserID).OnDelete(DeleteBehavior.Cascade).IsRequired();
            // TokenHash is looked up directly (never Email+code), and must be unique
            // so a hash collision can never resolve to more than one live token.
            entity.HasIndex(e => e.TokenHash).IsUnique();

            // Same reasoning as UserOtp: this flow runs before authentication
            // (BypassTenantFilter is true there), the filter only matters for
            // any future authenticated admin view over these tokens.
            entity.HasQueryFilter(e => BypassTenantFilter || e.User == null || e.User!.FirmID == RequestFirmId);
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

            // Cascading tenant filter via the owning User's FirmID. This is
            // the fix for a confirmed cross-tenant leak: GetAllLoginHistory
            // previously returned every firm's login IPs/emails to any Firm
            // Admin with the ViewLoginHistory permission. With this filter,
            // that same handler is now automatically scoped to one firm.
            entity.HasQueryFilter(e => BypassTenantFilter || e.User.FirmID == RequestFirmId);
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

            // ================================================================
            // CRITICAL FIX: AuditLog has no FirmID column, and until now had
            // NO tenant scoping whatsoever - GetAuditLogsHandler queried
            // _context.AuditLogs with zero FirmID filter. This was masked
            // only by "ViewAuditLogs" never being seeded to any role except
            // via SuperAdmin's permission-check bypass (see PermissionService)
            // - the instant that permission is granted to FirmAdmin (a very
            // natural-looking fix, and exactly what already happened for the
            // analogous "ViewLoginHistory" permission elsewhere in this
            // codebase), every firm's ENTIRE audit trail - case deletions,
            // user creation, permission/role changes, document activity -
            // becomes visible to every other firm's admin. Scope by the
            // acting user's firm at the model level so this can never regress
            // silently regardless of what the handler above does. Rows with
            // no UserID (system/platform-level events with no single firm
            // owner) are hidden from every non-SuperAdmin caller rather than
            // shown to all of them.
            // ================================================================
            entity.HasQueryFilter(e => BypassTenantFilter || (e.User != null && e.User.FirmID == RequestFirmId));
        });

        // ================================================================
        // ✅ COURT ENTITY CONFIGURATION (per-tenant scoping added - see
        // Models/Masters/Court.cs for the full rationale)
        // ================================================================
        modelBuilder.Entity<Court>(entity =>
        {
            entity.HasOne(e => e.Firm).WithMany().HasForeignKey(e => e.FirmID).OnDelete(DeleteBehavior.Restrict);

            // A caller sees system-wide global courts (FirmID null) plus
            // their own firm's custom courts. SuperAdmin bypasses and sees
            // every court, including every other firm's custom entries.
            entity.HasQueryFilter(e => BypassTenantFilter || e.FirmID == null || e.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ DEPARTMENT ENTITY CONFIGURATION (per-tenant scoping added - see
        // Models/Masters/Department.cs for the full rationale)
        // ================================================================
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasOne(e => e.Firm).WithMany().HasForeignKey(e => e.FirmID).OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => BypassTenantFilter || e.FirmID == null || e.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ CASECATEGORY / CASESTATUS / CASESTAGE / DOCUMENTTYPE ENTITY
        // CONFIGURATION (per-tenant scoping - same pattern as Court/Department)
        // ================================================================
        modelBuilder.Entity<CaseCategory>(entity =>
        {
            entity.HasOne(e => e.Firm).WithMany().HasForeignKey(e => e.FirmID).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => BypassTenantFilter || e.FirmID == null || e.FirmID == RequestFirmId);
        });

        modelBuilder.Entity<CaseStatus>(entity =>
        {
            entity.HasOne(e => e.Firm).WithMany().HasForeignKey(e => e.FirmID).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => BypassTenantFilter || e.FirmID == null || e.FirmID == RequestFirmId);
        });

        modelBuilder.Entity<CaseStage>(entity =>
        {
            entity.HasOne(e => e.Firm).WithMany().HasForeignKey(e => e.FirmID).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => BypassTenantFilter || e.FirmID == null || e.FirmID == RequestFirmId);
        });

        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.HasOne(e => e.Firm).WithMany().HasForeignKey(e => e.FirmID).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => BypassTenantFilter || e.FirmID == null || e.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ CASE ENTITY CONFIGURATION
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

            entity.HasOne(e => e.Firm).WithMany().HasForeignKey(e => e.FirmID).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Court).WithMany().HasForeignKey(e => e.CourtID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.StatusID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Stage).WithMany().HasForeignKey(e => e.StageID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.ResponsibleDepartmentID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.LegalOfficer).WithMany().HasForeignKey(e => e.CurrentLegalOfficerID).OnDelete(DeleteBehavior.Restrict);

            // Performance indexes
            entity.HasIndex(e => e.StatusID);
            entity.HasIndex(e => e.CourtID);
            entity.HasIndex(e => e.CategoryID);
            entity.HasIndex(e => e.CaseNumber);
            entity.HasIndex(e => e.FirmID);

            // Global tenant filter: a Case belongs to exactly one firm.
            entity.HasQueryFilter(e => BypassTenantFilter || e.FirmID == RequestFirmId);
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
            entity.HasIndex(e => e.CaseID);

            // Cascading tenant filter via the owning Case's FirmID.
            entity.HasQueryFilter(e => BypassTenantFilter || e.Case.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ CASE ASSIGNMENTS ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<CaseAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentID);
            entity.Property(e => e.AssignmentType).IsRequired().HasMaxLength(30);
            entity.Property(e => e.LeadCounsel).HasColumnName("IsLeadCounsel");
            entity.HasOne(e => e.Case).WithMany(c => c.CaseAssignments).HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserID).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.UserID);
            entity.HasIndex(e => new { e.CaseID, e.EndDate });

            // Cascading tenant filter via the owning Case's FirmID.
            entity.HasQueryFilter(e => BypassTenantFilter || e.Case.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ CASE STATUS HISTORY ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<CaseStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryID);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);

            // Cascading tenant filter via the owning Case's FirmID.
            entity.HasQueryFilter(e => BypassTenantFilter || e.Case.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ CASE MILESTONES ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<CaseMilestone>(entity =>
        {
            entity.HasKey(e => e.MilestoneID);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.CaseID);

            // Cascading tenant filter via the owning Case's FirmID.
            entity.HasQueryFilter(e => BypassTenantFilter || e.Case.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ HEARINGS ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<Hearing>(entity =>
        {
            entity.HasKey(e => e.HearingID);
            entity.HasOne(e => e.Case).WithMany(c => c.Hearings).HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Court).WithMany().HasForeignKey(e => e.CourtID).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.HearingAttendances).WithOne(ha => ha.Hearing).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.HearingDate);
            entity.HasIndex(e => e.CaseID);

            // Cascading tenant filter via the owning Case's FirmID.
            entity.HasQueryFilter(e => BypassTenantFilter || e.Case.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ HEARING ATTENDANCE ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<HearingAttendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceID);
            entity.HasOne(e => e.Hearing).WithMany(h => h.HearingAttendances).HasForeignKey(e => e.HearingID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserID).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.HearingID, e.UserID }).IsUnique();

            // Cascading tenant filter via Hearing -> Case's FirmID.
            entity.HasQueryFilter(e => BypassTenantFilter || e.Hearing.Case.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ DEADLINES ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<Deadline>(entity =>
        {
            entity.HasKey(e => e.DeadlineID);
            entity.HasOne(e => e.Case).WithMany(c => c.Deadlines).HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => new { e.CaseID, e.Completed });

            // Cascading tenant filter via the owning Case's FirmID.
            entity.HasQueryFilter(e => BypassTenantFilter || e.Case.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ DOCUMENTS ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocumentID);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.DocumentType).WithMany().HasForeignKey(e => e.DocumentTypeID).OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(e => e.DocumentPermissions).WithOne(dp => dp.Document).OnDelete(DeleteBehavior.Cascade);

            // Cascading tenant filter via the owning Case's FirmID. This is
            // the single most important filter in the file: it is what
            // stops a Moharrir/Lawyer/FirmAdmin from ever retrieving another
            // firm's document just by guessing/incrementing a DocumentID.
            entity.HasQueryFilter(e => BypassTenantFilter || e.Case.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ DOCUMENT PERMISSIONS ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<DocumentPermission>(entity =>
        {
            entity.HasKey(e => e.PermissionID);
            entity.HasOne(e => e.Document).WithMany(d => d.DocumentPermissions).HasForeignKey(e => e.DocumentID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Role).WithMany().HasForeignKey(e => e.RoleID).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserID).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.DocumentID, e.RoleID });
            entity.HasIndex(e => new { e.DocumentID, e.UserID });

            // Cascading tenant filter via Document -> Case's FirmID.
            entity.HasQueryFilter(e => BypassTenantFilter || e.Document.Case.FirmID == RequestFirmId);
        });

        // ================================================================
        // ✅ CASE NOTES ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<CaseNote>(entity =>
        {
            entity.HasKey(e => e.NoteID);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserID).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.CaseID);

            // Cascading tenant filter via the owning Case's FirmID.
            entity.HasQueryFilter(e => BypassTenantFilter || e.Case.FirmID == RequestFirmId);
        });
        // ================================================================
        modelBuilder.Entity<NotificationType>(entity =>
        {
            entity.HasKey(e => e.NotificationTypeID);
            entity.Property(e => e.TypeName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.TypeName).IsUnique();
        });

        // ================================================================
        // ✅ NOTIFICATIONS ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationID);
            entity.HasOne(e => e.NotificationType).WithMany(t => t.Notifications).HasForeignKey(e => e.NotificationTypeID).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.NoAction);
            entity.Property(e => e.Priority).HasMaxLength(20);

            // Cascading tenant filter via the notified User's FirmID (every
            // Notification always has a UserID; CaseID is optional so the
            // Case navigation alone isn't reliable for this filter).
            entity.HasQueryFilter(e => BypassTenantFilter || (e.User != null && e.User.FirmID == RequestFirmId));
        });

        // ================================================================================
        // ✅✅✅ SEED DATA - COMPLETE INITIALIZATION ====================================
        // ================================================================================
        SeedFirms(modelBuilder);
        SeedRoles(modelBuilder);
        SeedPermissions(modelBuilder);
        SeedRolePermissions(modelBuilder);
        SeedDepartments(modelBuilder);
        SeedCourts(modelBuilder);
        SeedCaseCategories(modelBuilder);
        SeedCaseStatus(modelBuilder);
        SeedCaseStages(modelBuilder);
        SeedDocumentTypes(modelBuilder);
        SeedUsers(modelBuilder);
        SeedNotificationTypes(modelBuilder);
    }

    // ====================================================================================
    // FIRMS - Multi-tenant workspace seeds
    // ====================================================================================
    // NOTE: Seed data (HasData) must be deterministic. Using DateTime.UtcNow here
    // (or as a property's default initializer) bakes a different value into the
    // model every single time the project is built, which makes EF Core think the
    // model has "pending changes" forever, even with no real schema change.
    // A fixed constant keeps the seeded rows stable across builds/migrations.
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static void SeedFirms(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Firm>().HasData(
            new Firm
            {
                FirmID = 1,
                FirmName = "Demo Law Firm",
                FirmCode = "DEMO",
                MigrationStatus = "None",
                MigrationNotes = "Development/Testing Firm",
                CreatedAt = SeedTimestamp
            },
            new Firm
            {
                FirmID = 2,
                FirmName = "Test Law Firm",
                FirmCode = "TEST",
                MigrationStatus = "None",
                MigrationNotes = "QA Testing Firm",
                CreatedAt = SeedTimestamp
            }
        );
    }

    // ====================================================================================
    // USERS - Complete seed users with all roles (using real BCrypt hashes)
    // ⚠️ All six seeded accounts below share the same demo password: Demo@12345
    // Each has its own distinct salt (hashes differ) even though the password matches.
    // These are DEV/DEMO credentials only - rotate or remove before any real deployment.
    // ====================================================================================
    // ====================================================================================
    // BUG FIX (CRITICAL): none of the six seeded demo users had RoleID set.
    // GetRole() therefore returned null for all of them, PermissionService
    // treated every one of them as roleless, and JwtService could not add a
    // Role claim to their tokens - meaning every seeded account, INCLUDING
    // the SuperAdmin, was locked out of every permission check out of the
    // box. RoleID is now set explicitly for each seeded user below.
    //
    // SecurityStamp values are fixed, deterministic strings (not
    // Guid.NewGuid()) for the same reason SeedTimestamp is a constant: HasData
    // requires stable seed values, or EF Core thinks the model has pending
    // changes on every build even with no real schema change.
    // ====================================================================================
    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            // SuperAdmin - Platform Owner
            new User
            {
                UserID = 1,
                Email = "superadmin@lts.pk",
                FullName = "Super Administrator",
                PasswordHash = "$2a$12$mnEYm2TirTnpNbZnz07S..gjd6klD5GFraAi5WJRqyr4yB1t0imd6", // Demo@12345
                FirmID = null,
                RoleID = (int)UserRole.SuperAdmin,
                Designation = "System Administrator",
                IsExternal = false,
                IsActive = true,
                SecurityStamp = "SEED-STAMP-USER-0001",
                CreatedAt = SeedTimestamp
            },
            // FirmAdmin - Demo Firm Manager
            new User
            {
                UserID = 2,
                Email = "admin@demolaw.pk",
                FullName = "Firm Administrator",
                PasswordHash = "$2a$12$aW90FxlGx4mqKoBvNUZ5TurErcGgJNpN2/r8wu/MsCI3LsN4Wrhte", // Demo@12345
                FirmID = 1,
                RoleID = (int)UserRole.FirmAdmin,
                Designation = "Firm Administrator",
                IsExternal = false,
                IsActive = true,
                SecurityStamp = "SEED-STAMP-USER-0002",
                CreatedAt = SeedTimestamp
            },
            // Partner - Senior Lawyer
            new User
            {
                UserID = 3,
                Email = "partner@demolaw.pk",
                FullName = "Muhammad Ashraf (Partner)",
                PasswordHash = "$2a$12$B7zvJrv3ubJs.W9M/QiCDO2ZkSo7q569cqUmCXBzyRGfJO14uIKRG", // Demo@12345
                FirmID = 1,
                RoleID = (int)UserRole.Partner,
                Designation = "Senior Partner",
                IsExternal = false,
                IsActive = true,
                SecurityStamp = "SEED-STAMP-USER-0003",
                CreatedAt = SeedTimestamp
            },
            // Associate Lawyer
            new User
            {
                UserID = 4,
                Email = "associate@demolaw.pk",
                FullName = "Ayesha Khan (Associate)",
                PasswordHash = "$2a$12$H76onrsOSDwWi3CGjGz4J.IJx7x5kKaCJ2Jk/fDxlEUfQhYrPDddC", // Demo@12345
                FirmID = 1,
                RoleID = (int)UserRole.AssociateLawyer,
                Designation = "Associate Lawyer",
                IsExternal = false,
                IsActive = true,
                SecurityStamp = "SEED-STAMP-USER-0004",
                CreatedAt = SeedTimestamp
            },
            // Moharrir - Legal Clerk
            new User
            {
                UserID = 5,
                Email = "moharrir@demolaw.pk",
                FullName = "Hassan Ali (Moharrir)",
                PasswordHash = "$2a$12$xBm7jXWO7osy9u4A2r1LmO606ZwKNNj6Lico4zq0ndcsew.WMx7ui", // Demo@12345
                FirmID = 1,
                RoleID = (int)UserRole.Moharrir,
                Designation = "Legal Clerk",
                IsExternal = false,
                IsActive = true,
                SecurityStamp = "SEED-STAMP-USER-0005",
                CreatedAt = SeedTimestamp
            },
            // Intern / Paralegal
            new User
            {
                UserID = 6,
                Email = "intern@demolaw.pk",
                FullName = "Amna Saeed (Intern)",
                PasswordHash = "$2a$12$53G3.jdH6VkrF.dRg9pdgOzSw28kvZo4y31V99DZ4Lii2oqVLNkXy", // Demo@12345
                FirmID = 1,
                RoleID = (int)UserRole.InternParalegal,
                Designation = "Paralegal Intern",
                IsExternal = false,
                IsActive = true,
                SecurityStamp = "SEED-STAMP-USER-0006",
                CreatedAt = SeedTimestamp
            }
        );
    }

    // ====================================================================================
    // DEPARTMENTS - Government/Organization departments
    // ====================================================================================
    private static void SeedDepartments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>().HasData(
            new Department { DepartmentID = 1, DepartmentName = "Finance Department", DepartmentCode = "FIN", IsActive = true },
            new Department { DepartmentID = 2, DepartmentName = "Revenue Department", DepartmentCode = "REV", IsActive = true },
            new Department { DepartmentID = 3, DepartmentName = "Law Department", DepartmentCode = "LAW", IsActive = true },
            new Department { DepartmentID = 4, DepartmentName = "Defense Department", DepartmentCode = "DEF", IsActive = true },
            new Department { DepartmentID = 5, DepartmentName = "Interior Department", DepartmentCode = "INT", IsActive = true }
        );
    }

    // ====================================================================================
    // COURTS - Pakistani courts hierarchy
    // ====================================================================================
    private static void SeedCourts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Court>().HasData(
            new Court { CourtID = 1, CourtName = "Supreme Court of Pakistan", CourtType = "Federal", Jurisdiction = "National", Address = "Constitution Avenue, Islamabad", IsActive = true, CreatedDate = SeedTimestamp },
            new Court { CourtID = 2, CourtName = "Islamabad High Court", CourtType = "High Court", Jurisdiction = "Islamabad Capital Territory", Address = "H-8/4, Islamabad", IsActive = true, CreatedDate = SeedTimestamp },
            new Court { CourtID = 3, CourtName = "Lahore High Court", CourtType = "High Court", Jurisdiction = "Punjab", Address = "The Mall, Lahore", IsActive = true, CreatedDate = SeedTimestamp },
            new Court { CourtID = 4, CourtName = "Sindh High Court", CourtType = "High Court", Jurisdiction = "Sindh", Address = "Constitution Avenue, Karachi", IsActive = true, CreatedDate = SeedTimestamp },
            new Court { CourtID = 5, CourtName = "Peshawar High Court", CourtType = "High Court", Jurisdiction = "Khyber Pakhtunkhwa", Address = "Peshawar", IsActive = true, CreatedDate = SeedTimestamp },
            new Court { CourtID = 6, CourtName = "Quetta High Court", CourtType = "High Court", Jurisdiction = "Balochistan", Address = "Quetta", IsActive = true, CreatedDate = SeedTimestamp },
            new Court { CourtID = 7, CourtName = "District Court Lahore", CourtType = "District Court", Jurisdiction = "Lahore District", Address = "Thokar Niaz Baig, Lahore", IsActive = true, CreatedDate = SeedTimestamp },
            new Court { CourtID = 8, CourtName = "District Court Karachi", CourtType = "District Court", Jurisdiction = "Karachi District", Address = "Karachi", IsActive = true, CreatedDate = SeedTimestamp }
        );
    }

    // ====================================================================================
    // CASE CATEGORIES - Types of litigation
    // ====================================================================================
    private static void SeedCaseCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CaseCategory>().HasData(
            new CaseCategory { CategoryID = 1, CategoryName = "Civil", Description = "Civil matters and disputes" },
            new CaseCategory { CategoryID = 2, CategoryName = "Criminal", Description = "Criminal cases" },
            new CaseCategory { CategoryID = 3, CategoryName = "Constitutional", Description = "Constitutional matters" },
            new CaseCategory { CategoryID = 4, CategoryName = "Corporate", Description = "Corporate and commercial disputes" },
            new CaseCategory { CategoryID = 5, CategoryName = "Labour", Description = "Labour and employment disputes" },
            new CaseCategory { CategoryID = 6, CategoryName = "Administrative", Description = "Administrative law matters" },
            new CaseCategory { CategoryID = 7, CategoryName = "Banking", Description = "Banking and financial disputes" },
            new CaseCategory { CategoryID = 8, CategoryName = "Tax", Description = "Tax-related matters" }
        );
    }

    // ====================================================================================
    // CASE STATUS - Case lifecycle statuses
    // ====================================================================================
    private static void SeedCaseStatus(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CaseStatus>().HasData(
            new CaseStatus { StatusID = 1, StatusName = "New", SequenceNo = 1, ColorCode = "#0066CC", IsClosed = false, IsActive = true },
            new CaseStatus { StatusID = 2, StatusName = "Pending", SequenceNo = 2, ColorCode = "#FF9900", IsClosed = false, IsActive = true },
            new CaseStatus { StatusID = 3, StatusName = "Active", SequenceNo = 3, ColorCode = "#00CC66", IsClosed = false, IsActive = true },
            new CaseStatus { StatusID = 4, StatusName = "Hearing Scheduled", SequenceNo = 4, ColorCode = "#FF6600", IsClosed = false, IsActive = true },
            new CaseStatus { StatusID = 5, StatusName = "Judgment Reserved", SequenceNo = 5, ColorCode = "#9900CC", IsClosed = false, IsActive = true },
            new CaseStatus { StatusID = 6, StatusName = "Closed", SequenceNo = 6, ColorCode = "#666666", IsClosed = true, IsActive = true },
            new CaseStatus { StatusID = 7, StatusName = "Archived", SequenceNo = 7, ColorCode = "#999999", IsClosed = true, IsActive = true }
        );
    }

    // ====================================================================================
    // CASE STAGES - Stages of litigation
    // ====================================================================================
    private static void SeedCaseStages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CaseStage>().HasData(
            new CaseStage { StageID = 1, StageName = "Filing", Description = "Initial case filing stage" },
            new CaseStage { StageID = 2, StageName = "Admission", Description = "Case admission by court" },
            new CaseStage { StageID = 3, StageName = "Evidence", Description = "Evidence submission stage" },
            new CaseStage { StageID = 4, StageName = "Arguments", Description = "Oral arguments before court" },
            new CaseStage { StageID = 5, StageName = "Judgment", Description = "Judgment delivery" },
            new CaseStage { StageID = 6, StageName = "Appeal", Description = "Appeal proceedings" }
        );
    }

    // ====================================================================================
    // DOCUMENT TYPES - Types of legal documents
    // ====================================================================================
    private static void SeedDocumentTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentType>().HasData(
            new DocumentType { DocumentTypeID = 1, TypeName = "Petition", Description = "Main petition/plaint document" },
            new DocumentType { DocumentTypeID = 2, TypeName = "Affidavit", Description = "Sworn affidavit" },
            new DocumentType { DocumentTypeID = 3, TypeName = "Court Order", Description = "Order issued by court" },
            new DocumentType { DocumentTypeID = 4, TypeName = "Evidence", Description = "Supporting evidence documents" },
            new DocumentType { DocumentTypeID = 5, TypeName = "Reply", Description = "Reply to petition/arguments" },
            new DocumentType { DocumentTypeID = 6, TypeName = "Judgment", Description = "Final judgment document" },
            new DocumentType { DocumentTypeID = 7, TypeName = "Notice", Description = "Legal notices" },
            new DocumentType { DocumentTypeID = 8, TypeName = "Appeal", Description = "Appeal documents" }
        );
    }

    // ====================================================================================
    // ROLES - 6 role levels in hierarchy
    // ====================================================================================
    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = (int)UserRole.SuperAdmin, RoleName = nameof(UserRole.SuperAdmin), Description = "System-wide management and data custody", IsSystemRole = true, IsActive = true },
            new Role { RoleID = (int)UserRole.FirmAdmin, RoleName = nameof(UserRole.FirmAdmin), Description = "Workspace owner - manages specific law firm", IsSystemRole = false, IsActive = true },
            new Role { RoleID = (int)UserRole.Partner, RoleName = nameof(UserRole.Partner), Description = "Senior lawyer - supervises case teams", IsSystemRole = false, IsActive = true },
            new Role { RoleID = (int)UserRole.AssociateLawyer, RoleName = nameof(UserRole.AssociateLawyer), Description = "Day-to-day legal work", IsSystemRole = false, IsActive = true },
            new Role { RoleID = (int)UserRole.Moharrir, RoleName = nameof(UserRole.Moharrir), Description = "Legal clerk / Data entry operator", IsSystemRole = false, IsActive = true },
            new Role { RoleID = (int)UserRole.InternParalegal, RoleName = nameof(UserRole.InternParalegal), Description = "Temporary staff / Junior assistant", IsSystemRole = false, IsActive = true }
        );
    }

    // ====================================================================================
    // PERMISSIONS - 30+ granular permissions
    // ====================================================================================
    private static void SeedPermissions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Permission>().HasData(
            // Super Admin Permissions
            new Permission { PermissionID = (int)PermissionEnum.ManageFirms, PermissionName = nameof(PermissionEnum.ManageFirms), Description = "Create, block, remove firms" },
            new Permission { PermissionID = (int)PermissionEnum.ViewSystemAuditLogs, PermissionName = nameof(PermissionEnum.ViewSystemAuditLogs), Description = "View system-wide audit logs" },
            new Permission { PermissionID = (int)PermissionEnum.ManageDataMigration, PermissionName = nameof(PermissionEnum.ManageDataMigration), Description = "Manage firm data migration" },
            new Permission { PermissionID = (int)PermissionEnum.ManageSystemUsers, PermissionName = nameof(PermissionEnum.ManageSystemUsers), Description = "Manage all system users" },

            // Firm Admin Permissions
            new Permission { PermissionID = (int)PermissionEnum.ManageFirmUsers, PermissionName = nameof(PermissionEnum.ManageFirmUsers), Description = "Create and manage firm users" },
            new Permission { PermissionID = (int)PermissionEnum.ViewFirmCaseDirectory, PermissionName = nameof(PermissionEnum.ViewFirmCaseDirectory), Description = "View all cases in firm" },
            new Permission { PermissionID = (int)PermissionEnum.AssignLawyersToCases, PermissionName = nameof(PermissionEnum.AssignLawyersToCases), Description = "Assign lawyers to cases" },
            new Permission { PermissionID = (int)PermissionEnum.ManageFirmSettings, PermissionName = nameof(PermissionEnum.ManageFirmSettings), Description = "Manage firm settings and billing" },
            new Permission { PermissionID = (int)PermissionEnum.DeleteCases, PermissionName = nameof(PermissionEnum.DeleteCases), Description = "Delete cases" },
            new Permission { PermissionID = (int)PermissionEnum.ViewLoginHistory, PermissionName = nameof(PermissionEnum.ViewLoginHistory), Description = "View this firm's own login history" },
            new Permission { PermissionID = (int)PermissionEnum.DeleteLoginHistory, PermissionName = nameof(PermissionEnum.DeleteLoginHistory), Description = "Delete/cleanup this firm's own login history records" },
            new Permission { PermissionID = (int)PermissionEnum.ViewAuditLogs, PermissionName = nameof(PermissionEnum.ViewAuditLogs), Description = "View this firm's own audit trail" },
            new Permission { PermissionID = (int)PermissionEnum.UploadDocuments, PermissionName = nameof(PermissionEnum.UploadDocuments), Description = "Upload documents" },

            // Partner/Senior Lawyer Permissions
            new Permission { PermissionID = (int)PermissionEnum.ViewFirmCases, PermissionName = nameof(PermissionEnum.ViewFirmCases), Description = "View firm cases" },
            new Permission { PermissionID = (int)PermissionEnum.CreateCases, PermissionName = nameof(PermissionEnum.CreateCases), Description = "Create new cases" },
            new Permission { PermissionID = (int)PermissionEnum.UpdateCases, PermissionName = nameof(PermissionEnum.UpdateCases), Description = "Update case information" },
            new Permission { PermissionID = (int)PermissionEnum.AssignCases, PermissionName = nameof(PermissionEnum.AssignCases), Description = "Assign cases to lawyers" },
            new Permission { PermissionID = (int)PermissionEnum.ViewAllDocuments, PermissionName = nameof(PermissionEnum.ViewAllDocuments), Description = "View all case documents" },
            new Permission { PermissionID = (int)PermissionEnum.DownloadDocuments, PermissionName = nameof(PermissionEnum.DownloadDocuments), Description = "Download documents" },
            new Permission { PermissionID = (int)PermissionEnum.ApproveFilings, PermissionName = nameof(PermissionEnum.ApproveFilings), Description = "Approve critical filings" },
            new Permission { PermissionID = (int)PermissionEnum.ViewFirmAnalytics, PermissionName = nameof(PermissionEnum.ViewFirmAnalytics), Description = "View firm analytics and reports" },

            // Associate Lawyer Permissions
            new Permission { PermissionID = (int)PermissionEnum.ViewAssignedCases, PermissionName = nameof(PermissionEnum.ViewAssignedCases), Description = "View assigned cases only" },
            new Permission { PermissionID = (int)PermissionEnum.DownloadAssignedDocuments, PermissionName = nameof(PermissionEnum.DownloadAssignedDocuments), Description = "Download assigned case documents" },
            new Permission { PermissionID = (int)PermissionEnum.AddCaseNotes, PermissionName = nameof(PermissionEnum.AddCaseNotes), Description = "Add notes to cases" },
            new Permission { PermissionID = (int)PermissionEnum.TrackDeadlines, PermissionName = nameof(PermissionEnum.TrackDeadlines), Description = "Track case deadlines" },
            new Permission { PermissionID = (int)PermissionEnum.LogBillableHours, PermissionName = nameof(PermissionEnum.LogBillableHours), Description = "Log billable hours" },

            // Moharrir Permissions
            new Permission { PermissionID = (int)PermissionEnum.EnterCaseData, PermissionName = nameof(PermissionEnum.EnterCaseData), Description = "Enter case data" },
            new Permission { PermissionID = (int)PermissionEnum.UploadCaseDocuments, PermissionName = nameof(PermissionEnum.UploadCaseDocuments), Description = "Upload case documents" },
            new Permission { PermissionID = (int)PermissionEnum.ViewDocumentsIfPermitted, PermissionName = nameof(PermissionEnum.ViewDocumentsIfPermitted), Description = "View documents if permitted" },
            new Permission { PermissionID = (int)PermissionEnum.DownloadDocumentsIfPermitted, PermissionName = nameof(PermissionEnum.DownloadDocumentsIfPermitted), Description = "Download documents if permitted" },
            new Permission { PermissionID = (int)PermissionEnum.MaintainCaseRecords, PermissionName = nameof(PermissionEnum.MaintainCaseRecords), Description = "Maintain case records" },

            // Intern/Paralegal Permissions
            new Permission { PermissionID = (int)PermissionEnum.ViewDocumentsReadOnly, PermissionName = nameof(PermissionEnum.ViewDocumentsReadOnly), Description = "View documents (read-only)" },
            new Permission { PermissionID = (int)PermissionEnum.DraftDocuments, PermissionName = nameof(PermissionEnum.DraftDocuments), Description = "Draft legal documents" },
            new Permission { PermissionID = (int)PermissionEnum.PerformResearch, PermissionName = nameof(PermissionEnum.PerformResearch), Description = "Perform legal research" },

            // Cross-role: every role has its own dashboard
            new Permission { PermissionID = (int)PermissionEnum.ViewDashboard, PermissionName = nameof(PermissionEnum.ViewDashboard), Description = "View one's own role-scoped dashboard" }
        );
    }

    // ====================================================================================
    // ROLE-PERMISSIONS - Complete matrix mapping roles to permissions
    // ====================================================================================
    private static void SeedRolePermissions(ModelBuilder modelBuilder)
    {
        var rolePermissions = new List<RolePermission>();
        int id = 1;

        void Map(UserRole role, params PermissionEnum[] permissions)
        {
            foreach (var p in permissions)
            {
                rolePermissions.Add(new RolePermission
                {
                    RolePermissionID = id++,
                    RoleID = (int)role,
                    PermissionID = (int)p
                });
            }
        }

        Map(UserRole.SuperAdmin,
            PermissionEnum.ManageFirms,
            PermissionEnum.ViewSystemAuditLogs,
            PermissionEnum.ManageDataMigration,
            PermissionEnum.ManageSystemUsers);

        Map(UserRole.FirmAdmin,
            PermissionEnum.ManageFirmUsers,
            PermissionEnum.ViewFirmCaseDirectory,
            PermissionEnum.AssignLawyersToCases,
            PermissionEnum.ManageFirmSettings,
            PermissionEnum.DeleteCases,
            PermissionEnum.ViewFirmCases,
            PermissionEnum.CreateCases,
            PermissionEnum.UpdateCases,
            PermissionEnum.ViewAllDocuments,
            PermissionEnum.DownloadDocuments,
            PermissionEnum.UploadDocuments,
            PermissionEnum.ViewFirmAnalytics,
            // BUG FIX: LoginHistoryController required this permission but it
            // never existed anywhere in the seed data - see PermissionEnum.cs.
            // DeleteLoginHistory is intentionally NOT granted here (SuperAdmin
            // only) to keep this security-relevant audit trail tamper-resistant.
            PermissionEnum.ViewLoginHistory,
            // BUG FIX: same gap as ViewLoginHistory above - AuditLogsController
            // and DashboardController required these but neither was ever
            // seeded, so only SuperAdmin (via the old blanket bypass) could
            // reach them. Both are firm-scoped for FirmAdmin (see
            // GetAuditLogsHandler / GetFirmDashboardHandler).
            PermissionEnum.ViewAuditLogs,
            PermissionEnum.ViewDashboard);

        Map(UserRole.Partner,
            PermissionEnum.ViewFirmCaseDirectory,
            PermissionEnum.AssignLawyersToCases,
            PermissionEnum.AssignCases,
            PermissionEnum.DeleteCases,
            PermissionEnum.ViewFirmCases,
            PermissionEnum.CreateCases,
            PermissionEnum.UpdateCases,
            PermissionEnum.ViewAllDocuments,
            PermissionEnum.DownloadDocuments,
            PermissionEnum.UploadDocuments,
            PermissionEnum.ApproveFilings,
            PermissionEnum.ViewFirmAnalytics,
            PermissionEnum.ViewDashboard);

        Map(UserRole.AssociateLawyer,
            PermissionEnum.ViewAssignedCases,
            PermissionEnum.UploadDocuments,
            PermissionEnum.DownloadAssignedDocuments,
            PermissionEnum.AddCaseNotes,
            PermissionEnum.TrackDeadlines,
            PermissionEnum.LogBillableHours,
            PermissionEnum.ViewDashboard);

        Map(UserRole.Moharrir,
            PermissionEnum.EnterCaseData,
            PermissionEnum.UploadCaseDocuments,
            PermissionEnum.MaintainCaseRecords,
            PermissionEnum.ViewDashboard);

        Map(UserRole.InternParalegal,
            PermissionEnum.ViewDocumentsReadOnly,
            PermissionEnum.DraftDocuments,
            PermissionEnum.PerformResearch,
            PermissionEnum.ViewDashboard);

        modelBuilder.Entity<RolePermission>().HasData(rolePermissions);
    }

    // ====================================================================================
    // NOTIFICATION TYPES - Automated notification triggers
    // ====================================================================================
    private static void SeedNotificationTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationType>().HasData(
            new NotificationType
            {
                NotificationTypeID = 1,
                TypeName = "DeadlineAlert",
                Description = "Reminder for approaching case deadline",
                IsEmail = true,
                IsSMS = false,
                IsInApp = true,
                IsActive = true
            },
            new NotificationType
            {
                NotificationTypeID = 2,
                TypeName = "HearingReminder",
                Description = "Reminder for upcoming court hearing",
                IsEmail = true,
                IsSMS = false,
                IsInApp = true,
                IsActive = true
            },
            new NotificationType
            {
                NotificationTypeID = 3,
                TypeName = "CaseAssignment",
                Description = "Notification when case is assigned",
                IsEmail = true,
                IsSMS = false,
                IsInApp = true,
                IsActive = true
            },
            new NotificationType
            {
                NotificationTypeID = 4,
                TypeName = "DocumentUploaded",
                Description = "Notification when document uploaded to case",
                IsEmail = false,
                IsSMS = false,
                IsInApp = true,
                IsActive = true
            },
            new NotificationType
            {
                NotificationTypeID = 5,
                TypeName = "CaseStatusChanged",
                Description = "Notification when case status changes",
                IsEmail = true,
                IsSMS = false,
                IsInApp = true,
                IsActive = true
            }
        );
    }
}