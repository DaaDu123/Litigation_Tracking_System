using Microsoft.EntityFrameworkCore;
using LTSBackend.Models;

namespace LTSBackend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserOtp> UserOtps => Set<UserOtp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite Key for Join Table
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleID, rp.PermissionID });

        // RolePermission Configuration
        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionID)
            .OnDelete(DeleteBehavior.Cascade);

        // RefreshToken Configuration
        modelBuilder.Entity<RefreshToken>()
            .HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // User & Role Configuration
        modelBuilder.Entity<User>()
            .HasOne(x => x.Role)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.RoleID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        // =====================================================
        // FIXED: UserOtp Relationship Configuration
        // =====================================================
        modelBuilder.Entity<UserOtp>()
            .HasOne(x => x.User)
            .WithMany(x => x.UserOtps)
            .HasForeignKey(x => x.UserID)
            .OnDelete(DeleteBehavior.SetNull);

        // Also ensure User has UserOtps collection
        modelBuilder.Entity<User>()
            .HasMany(x => x.UserOtps)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserID)
            .OnDelete(DeleteBehavior.SetNull);

        // =====================================================
        // MASTER SEED DATA
        // =====================================================

        // Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = 1, RoleName = "Admin", Description = "System Administrator" },
            new Role { RoleID = 2, RoleName = "Lawyer", Description = "Litigation Lawyer" },
            new Role { RoleID = 3, RoleName = "Clerk", Description = "Case Clerk" },
            new Role { RoleID = 4, RoleName = "Operator", Description = "System Operator" }
        );

        // Permissions - WITH ViewDashboard
        modelBuilder.Entity<Permission>().HasData(
            new Permission { PermissionID = 1, PermissionName = "ViewUsers" },
            new Permission { PermissionID = 2, PermissionName = "CreateUsers" },
            new Permission { PermissionID = 3, PermissionName = "UpdateUsers" },
            new Permission { PermissionID = 4, PermissionName = "DeleteUsers" },
            new Permission { PermissionID = 5, PermissionName = "ManageRoles" },
            new Permission { PermissionID = 6, PermissionName = "ViewAuditLogs" },
            new Permission { PermissionID = 7, PermissionName = "ViewDashboard" }
        );

        // RolePermissions - CORRECTED SYNTAX
        modelBuilder.Entity<RolePermission>().HasData(
            // ADMIN (All Permissions)
            new RolePermission { RoleID = 1, PermissionID = 1 },
            new RolePermission { RoleID = 1, PermissionID = 2 },
            new RolePermission { RoleID = 1, PermissionID = 3 },
            new RolePermission { RoleID = 1, PermissionID = 4 },
            new RolePermission { RoleID = 1, PermissionID = 5 },
            new RolePermission { RoleID = 1, PermissionID = 6 },
            new RolePermission { RoleID = 1, PermissionID = 7 },

            // LAWYER
            new RolePermission { RoleID = 2, PermissionID = 1 },
            new RolePermission { RoleID = 2, PermissionID = 3 },

            // CLERK
            new RolePermission { RoleID = 3, PermissionID = 1 },
            new RolePermission { RoleID = 3, PermissionID = 2 },

            // OPERATOR
            new RolePermission { RoleID = 4, PermissionID = 1 }
        );
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is User user)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        user.CreatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        user.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }

            if (entry.Entity is AuditLog log && entry.State == EntityState.Added)
                log.Timestamp = DateTime.UtcNow;
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}