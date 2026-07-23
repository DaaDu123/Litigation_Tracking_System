namespace LTSBackend.Comman.Enum;

public enum PermissionEnum
{
    // ===== SUPER ADMIN PERMISSIONS =====
    /// <summary>
    /// Create/Block/Remove Firms
    /// </summary>
    ManageFirms = 101,

    /// <summary>
    /// View system-wide audit logs
    /// </summary>
    ViewSystemAuditLogs = 102,

    /// <summary>
    /// Data migration and backup export
    /// </summary>
    ManageDataMigration = 103,

    /// <summary>
    /// Manage system-wide users
    /// </summary>
    ManageSystemUsers = 104,

    // ===== FIRM ADMIN PERMISSIONS =====
    /// <summary>
    /// Create and manage firm users
    /// </summary>
    ManageFirmUsers = 201,

    /// <summary>
    /// View all firm cases (directory)
    /// </summary>
    ViewFirmCaseDirectory = 202,

    /// <summary>
    /// Assign/Remove lawyers to cases
    /// </summary>
    AssignLawyersToCases = 203,

    /// <summary>
    /// Manage firm settings and billing
    /// </summary>
    ManageFirmSettings = 204,

    /// <summary>
    /// Delete cases
    /// </summary>
    DeleteCases = 205,

    // ===== PARTNER / SENIOR LAWYER =====
    /// <summary>
    /// View firm-wide case portfolio
    /// </summary>
    ViewFirmCases = 301,

    /// <summary>
    /// Create new cases
    /// </summary>
    CreateCases = 302,

    /// <summary>
    /// Update case details
    /// </summary>
    UpdateCases = 303,

    /// <summary>
    /// Assign lawyers to cases
    /// </summary>
    AssignCases = 304,

    /// <summary>
    /// View all documents (firm-wide)
    /// </summary>
    ViewAllDocuments = 305,

    /// <summary>
    /// Download documents
    /// </summary>
    DownloadDocuments = 306,

    /// <summary>
    /// Approve critical filings
    /// </summary>
    ApproveFilings = 307,

    /// <summary>
    /// View firm analytics
    /// </summary>
    ViewFirmAnalytics = 308,

    // ===== ASSOCIATE LAWYER =====
    /// <summary>
    /// View assigned cases only
    /// </summary>
    ViewAssignedCases = 401,

    /// <summary>
    /// Upload documents to cases
    /// </summary>
    UploadDocuments = 402,

    /// <summary>
    /// Download documents from assigned cases
    /// </summary>
    DownloadAssignedDocuments = 403,

    /// <summary>
    /// Add case notes and updates
    /// </summary>
    AddCaseNotes = 404,

    /// <summary>
    /// Track deadlines
    /// </summary>
    TrackDeadlines = 405,

    /// <summary>
    /// Log billable hours
    /// </summary>
    LogBillableHours = 406,

    // ===== MOHARRIR (LEGAL CLERK) =====
    /// <summary>
    /// Data entry - court diaries, hearing dates
    /// </summary>
    EnterCaseData = 501,

    /// <summary>
    /// Upload documents (write-only for restricted)
    /// </summary>
    UploadCaseDocuments = 502,

    /// <summary>
    /// View documents (conditional - only if permission granted)
    /// </summary>
    ViewDocumentsIfPermitted = 503,

    /// <summary>
    /// Download documents (conditional - only if permission granted)
    /// </summary>
    DownloadDocumentsIfPermitted = 504,

    /// <summary>
    /// Maintain case records
    /// </summary>
    MaintainCaseRecords = 505,

    // ===== INTERN / PARALEGAL =====
    /// <summary>
    /// View documents read-only
    /// </summary>
    ViewDocumentsReadOnly = 601,

    /// <summary>
    /// Draft documents (create, not publish)
    /// </summary>
    DraftDocuments = 602,

    /// <summary>
    /// Assist with research
    /// </summary>
    PerformResearch = 603
}