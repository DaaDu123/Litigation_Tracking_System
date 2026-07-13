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
        // ✅ ROLEPERMISSION ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(x => new { x.RoleID, x.PermissionID });
            entity.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleID);
            entity.HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionID);
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

            // Foreign Keys
            entity.HasOne(e => e.Court).WithMany().HasForeignKey(e => e.CourtID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryID);
            entity.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.StatusID);
            entity.HasOne(e => e.Stage).WithMany().HasForeignKey(e => e.StageID);
            entity.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.ResponsibleDepartmentID);
            entity.HasOne(e => e.LegalOfficer).WithMany().HasForeignKey(e => e.CurrentLegalOfficerID).OnDelete(DeleteBehavior.Restrict);

            // Relationships (One Case -> Many Children)
            entity.HasMany(e => e.CaseParties).WithOne(p => p.Case).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Hearings).WithOne(h => h.Case).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Deadlines).WithOne(d => d.Case).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.CaseAssignments).WithOne(a => a.Case).OnDelete(DeleteBehavior.Cascade);
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
            entity.HasOne(e => e.Court).WithMany().HasForeignKey(e => e.CourtID);
            entity.HasMany(e => e.HearingAttendances).WithOne(ha => ha.Hearing).OnDelete(DeleteBehavior.Cascade);
        });

        // ================================================================
        // ✅ HEARING ATTENDANCE ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<HearingAttendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceID);
            entity.HasOne(e => e.Hearing).WithMany(h => h.HearingAttendances).HasForeignKey(e => e.HearingID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserID);
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
            entity.HasOne(e => e.DocumentType).WithMany().HasForeignKey(e => e.DocumentTypeID);
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
            entity.HasOne(e => e.Role).WithMany().HasForeignKey(e => e.RoleID);
        });

        // ================================================================
        // ✅ CASE NOTES ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<CaseNote>(entity =>
        {
            entity.HasKey(e => e.NoteID);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserID);
        });

        // ================================================================
        // ✅ NOTIFICATIONS ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationID);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.SetNull);
        });
    }
}