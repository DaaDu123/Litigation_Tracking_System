using LTSBackend.Models.Audit;
using LTSBackend.Models.Cases;
using LTSBackend.Models.Masters;
using LTSBackend.Models.Security;
using Microsoft.EntityFrameworkCore;
using PermissionEnum = LTSBackend.Comman.Enum.Permission;   // ✅ enum, aliased to avoid clash with entity class Permission
using UserRole = LTSBackend.Comman.Enum.UserRole;            // ✅ explicit alias (blanket "using Comman.Enum" removed on purpose)

namespace LTSBackend.Data;

/// <summary>
/// ✅ AppDbContext: Main Database Context
/// 
/// Tamam tables ka configuration EF Core ke through
/// Isme sab DbSets, relationships, constraints aur seed data define hain
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
    public DbSet<NotificationType> NotificationTypes { get; set; } = null!;
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

            entity.HasOne(e => e.Court).WithMany().HasForeignKey(e => e.CourtID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.StatusID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Stage).WithMany().HasForeignKey(e => e.StageID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.ResponsibleDepartmentID).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.LegalOfficer).WithMany().HasForeignKey(e => e.CurrentLegalOfficerID).OnDelete(DeleteBehavior.Restrict);
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
            entity.Property(e => e.LeadCounsel).HasColumnName("IsLeadCounsel");
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
        // ✅ DOCUMENTS ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocumentID);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.DocumentType).WithMany().HasForeignKey(e => e.DocumentTypeID).OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(e => e.DocumentPermissions).WithOne(dp => dp.Document).OnDelete(DeleteBehavior.Cascade);
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
        });

        // ================================================================
        // ✅ CASE NOTES ENTITY CONFIGURATION
        // ================================================================
        modelBuilder.Entity<CaseNote>(entity =>
        {
            entity.HasKey(e => e.NoteID);
            entity.HasOne(e => e.Case).WithMany().HasForeignKey(e => e.CaseID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserID).OnDelete(DeleteBehavior.Restrict);
        });

        // ================================================================
        // ✅ NOTIFICATION TYPE ENTITY CONFIGURATION
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
        });

        // ================================================================================
        // ✅✅✅ SEED DATA ================================================================
        // ================================================================================
        SeedRoles(modelBuilder);
        SeedPermissions(modelBuilder);
        SeedRolePermissions(modelBuilder);
        SeedNotificationTypes(modelBuilder);
    }

    // ====================================================================================
    // ROLES — UserRole enum ke exact values ke sath (1-6)
    // ====================================================================================
    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = (int)UserRole.SuperAdmin, RoleName = nameof(UserRole.SuperAdmin), Description = "System-wide management and data custody" },
            new Role { RoleID = (int)UserRole.FirmAdmin, RoleName = nameof(UserRole.FirmAdmin), Description = "Workspace owner - manages specific law firm" },
            new Role { RoleID = (int)UserRole.Partner, RoleName = nameof(UserRole.Partner), Description = "Senior lawyer - supervises case teams" },
            new Role { RoleID = (int)UserRole.AssociateLawyer, RoleName = nameof(UserRole.AssociateLawyer), Description = "Day-to-day legal work" },
            new Role { RoleID = (int)UserRole.Moharrir, RoleName = nameof(UserRole.Moharrir), Description = "Legal clerk / Data entry operator" },
            new Role { RoleID = (int)UserRole.InternParalegal, RoleName = nameof(UserRole.InternParalegal), Description = "Temporary staff / Junior assistant" }
        );
    }

    // ====================================================================================
    // PERMISSIONS — entity rows, IDs Comman.Enum.Permission (aliased as PermissionEnum)
    // ke exact values se liye gaye hain
    // ====================================================================================
    private static void SeedPermissions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Permission>().HasData(
            // Super Admin
            new Permission { PermissionID = (int)PermissionEnum.ManageFirms, PermissionName = nameof(PermissionEnum.ManageFirms) },
            new Permission { PermissionID = (int)PermissionEnum.ViewSystemAuditLogs, PermissionName = nameof(PermissionEnum.ViewSystemAuditLogs) },
            new Permission { PermissionID = (int)PermissionEnum.ManageDataMigration, PermissionName = nameof(PermissionEnum.ManageDataMigration) },
            new Permission { PermissionID = (int)PermissionEnum.ManageSystemUsers, PermissionName = nameof(PermissionEnum.ManageSystemUsers) },

            // Firm Admin
            new Permission { PermissionID = (int)PermissionEnum.ManageFirmUsers, PermissionName = nameof(PermissionEnum.ManageFirmUsers) },
            new Permission { PermissionID = (int)PermissionEnum.ViewFirmCaseDirectory, PermissionName = nameof(PermissionEnum.ViewFirmCaseDirectory) },
            new Permission { PermissionID = (int)PermissionEnum.AssignLawyersToCases, PermissionName = nameof(PermissionEnum.AssignLawyersToCases) },
            new Permission { PermissionID = (int)PermissionEnum.ManageFirmSettings, PermissionName = nameof(PermissionEnum.ManageFirmSettings) },
            new Permission { PermissionID = (int)PermissionEnum.DeleteCases, PermissionName = nameof(PermissionEnum.DeleteCases) },

            // Partner / Senior Lawyer
            new Permission { PermissionID = (int)PermissionEnum.ViewFirmCases, PermissionName = nameof(PermissionEnum.ViewFirmCases) },
            new Permission { PermissionID = (int)PermissionEnum.CreateCases, PermissionName = nameof(PermissionEnum.CreateCases) },
            new Permission { PermissionID = (int)PermissionEnum.UpdateCases, PermissionName = nameof(PermissionEnum.UpdateCases) },
            new Permission { PermissionID = (int)PermissionEnum.AssignCases, PermissionName = nameof(PermissionEnum.AssignCases) },
            new Permission { PermissionID = (int)PermissionEnum.ViewAllDocuments, PermissionName = nameof(PermissionEnum.ViewAllDocuments) },
            new Permission { PermissionID = (int)PermissionEnum.DownloadDocuments, PermissionName = nameof(PermissionEnum.DownloadDocuments) },
            new Permission { PermissionID = (int)PermissionEnum.ApproveFilings, PermissionName = nameof(PermissionEnum.ApproveFilings) },
            new Permission { PermissionID = (int)PermissionEnum.ViewFirmAnalytics, PermissionName = nameof(PermissionEnum.ViewFirmAnalytics) },

            // Associate Lawyer
            new Permission { PermissionID = (int)PermissionEnum.ViewAssignedCases, PermissionName = nameof(PermissionEnum.ViewAssignedCases) },
            new Permission { PermissionID = (int)PermissionEnum.UploadDocuments, PermissionName = nameof(PermissionEnum.UploadDocuments) },
            new Permission { PermissionID = (int)PermissionEnum.DownloadAssignedDocuments, PermissionName = nameof(PermissionEnum.DownloadAssignedDocuments) },
            new Permission { PermissionID = (int)PermissionEnum.AddCaseNotes, PermissionName = nameof(PermissionEnum.AddCaseNotes) },
            new Permission { PermissionID = (int)PermissionEnum.TrackDeadlines, PermissionName = nameof(PermissionEnum.TrackDeadlines) },
            new Permission { PermissionID = (int)PermissionEnum.LogBillableHours, PermissionName = nameof(PermissionEnum.LogBillableHours) },

            // Moharrir
            new Permission { PermissionID = (int)PermissionEnum.EnterCaseData, PermissionName = nameof(PermissionEnum.EnterCaseData) },
            new Permission { PermissionID = (int)PermissionEnum.UploadCaseDocuments, PermissionName = nameof(PermissionEnum.UploadCaseDocuments) },
            new Permission { PermissionID = (int)PermissionEnum.ViewDocumentsIfPermitted, PermissionName = nameof(PermissionEnum.ViewDocumentsIfPermitted) },
            new Permission { PermissionID = (int)PermissionEnum.DownloadDocumentsIfPermitted, PermissionName = nameof(PermissionEnum.DownloadDocumentsIfPermitted) },
            new Permission { PermissionID = (int)PermissionEnum.MaintainCaseRecords, PermissionName = nameof(PermissionEnum.MaintainCaseRecords) },

            // Intern / Paralegal
            new Permission { PermissionID = (int)PermissionEnum.ViewDocumentsReadOnly, PermissionName = nameof(PermissionEnum.ViewDocumentsReadOnly) },
            new Permission { PermissionID = (int)PermissionEnum.DraftDocuments, PermissionName = nameof(PermissionEnum.DraftDocuments) },
            new Permission { PermissionID = (int)PermissionEnum.PerformResearch, PermissionName = nameof(PermissionEnum.PerformResearch) }
        );
    }

    // ====================================================================================
    // ROLE-PERMISSIONS — role matrix (Image 2) ke mutabiq
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
            PermissionEnum.ViewFirmAnalytics);

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
            PermissionEnum.ViewFirmAnalytics);

        Map(UserRole.AssociateLawyer,
            PermissionEnum.ViewAssignedCases,
            PermissionEnum.UploadDocuments,
            PermissionEnum.DownloadAssignedDocuments,
            PermissionEnum.AddCaseNotes,
            PermissionEnum.TrackDeadlines,
            PermissionEnum.LogBillableHours);

        Map(UserRole.Moharrir,
            PermissionEnum.EnterCaseData,
            PermissionEnum.UploadCaseDocuments,
            PermissionEnum.MaintainCaseRecords);

        Map(UserRole.InternParalegal,
            PermissionEnum.ViewDocumentsReadOnly,
            PermissionEnum.DraftDocuments,
            PermissionEnum.PerformResearch);

        modelBuilder.Entity<RolePermission>().HasData(rolePermissions);
    }

    // ====================================================================================
    // NOTIFICATION TYPES — ReminderService in dono types ko use karta hai
    // ====================================================================================
    private static void SeedNotificationTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationType>().HasData(
            new NotificationType
            {
                NotificationTypeID = 1,
                TypeName = "DeadlineAlert",
                Description = "Reminder for an approaching case deadline",
                IsEmail = true,
                IsSMS = false,
                IsInApp = true,
                IsActive = true
            },
            new NotificationType
            {
                NotificationTypeID = 2,
                TypeName = "HearingReminder",
                Description = "Reminder for an upcoming court hearing",
                IsEmail = true,
                IsSMS = false,
                IsInApp = true,
                IsActive = true
            },
            new NotificationType
            {
                NotificationTypeID = 3,
                TypeName = "CaseAssignment",
                Description = "Notification when a case is assigned to a user",
                IsEmail = true,
                IsSMS = false,
                IsInApp = true,
                IsActive = true
            },
            new NotificationType
            {
                NotificationTypeID = 4,
                TypeName = "DocumentUploaded",
                Description = "Notification when a new document is uploaded to a case",
                IsEmail = false,
                IsSMS = false,
                IsInApp = true,
                IsActive = true
            }
        );
    }
}