using LTSBackend.Data;
using LTSBackend.Models.Cases;
using LTSBackend.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Services.BackgroundServices;

public class ReminderService(IServiceScopeFactory scopeFactory, ILogger<ReminderService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    private const string DeadlineAlertType = "DeadlineAlert";
    private const string HearingReminderType = "HearingReminder";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating reminders");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task GenerateRemindersAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var today = DateTime.UtcNow.Date;

        // ================================================
        // Resolve NotificationType IDs (seeded rows)
        // ================================================
        var deadlineTypeId = await context.NotificationTypes
            .Where(t => t.TypeName == DeadlineAlertType)
            .Select(t => (int?)t.NotificationTypeID)
            .FirstOrDefaultAsync(ct);

        var hearingTypeId = await context.NotificationTypes
            .Where(t => t.TypeName == HearingReminderType)
            .Select(t => (int?)t.NotificationTypeID)
            .FirstOrDefaultAsync(ct);

        if (deadlineTypeId == null || hearingTypeId == null)
        {
            logger.LogError("NotificationType seed rows missing ({DeadlineType}/{HearingType}) — skipping reminder sweep",
                DeadlineAlertType, HearingReminderType);
            return;
        }

        // ================================================
        // DEADLINE REMINDERS
        // ================================================
        var dueDeadlines = await context.Deadlines
                 .Where(d => !d.Completed && d.DueDate.AddDays(-d.ReminderDays) <= today && d.DueDate >= today).ToListAsync(ct);

        foreach (var deadline in dueDeadlines)
        {
            var assignedUserIds = await context.CaseAssignments
                .Where(a => a.CaseID == deadline.CaseID && (a.EndDate == null || a.EndDate > DateTime.UtcNow))
                .Select(a => a.UserID)
                .ToListAsync(ct);

            foreach (var userId in assignedUserIds)
            {
                bool alreadyNotified = await context.Notifications.AnyAsync(n =>
                    n.NotificationTypeID == deadlineTypeId &&
                    n.CaseID == deadline.CaseID &&
                    n.UserID == userId &&
                    n.CreatedDate.Date == today, ct);

                if (alreadyNotified) continue;

                context.Notifications.Add(new Notification
                {
                    NotificationTypeID = deadlineTypeId.Value,
                    UserID = userId,
                    CaseID = deadline.CaseID,
                    Subject = "Deadline approaching",
                    Message = $"Deadline '{deadline.DeadlineType}' is due on {deadline.DueDate:yyyy-MM-dd}.",
                    Priority = "High",
                    IsRead = false,
                    IsSent = false,
                    CreatedDate = DateTime.UtcNow
                });
            }
        }

        // ================================================
        // HEARING REMINDERS (next 2 days)
        // ================================================
        var upcomingHearings = await context.Hearings
            .Where(h => h.HearingDate.Date >= today && h.HearingDate.Date <= today.AddDays(2))
            .ToListAsync(ct);

        foreach (var hearing in upcomingHearings)
        {
            var assignedUserIds = await context.CaseAssignments
                .Where(a => a.CaseID == hearing.CaseID && (a.EndDate == null || a.EndDate > DateTime.UtcNow))
                .Select(a => a.UserID)
                .ToListAsync(ct);

            foreach (var userId in assignedUserIds)
            {
                bool alreadyNotified = await context.Notifications.AnyAsync(n =>
                    n.NotificationTypeID == hearingTypeId &&
                    n.CaseID == hearing.CaseID &&
                    n.UserID == userId &&
                    n.CreatedDate.Date == today, ct);

                if (alreadyNotified) continue;

                context.Notifications.Add(new Notification
                {
                    NotificationTypeID = hearingTypeId.Value,
                    UserID = userId,
                    CaseID = hearing.CaseID,
                    Subject = "Upcoming hearing",
                    Message = $"Hearing scheduled on {hearing.HearingDate:yyyy-MM-dd}.",
                    Priority = "Critical",
                    IsRead = false,
                    IsSent = false,
                    CreatedDate = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Reminder sweep complete: {Deadlines} deadlines, {Hearings} hearings checked",
            dueDeadlines.Count, upcomingHearings.Count);
    }
}