using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nocturne.Infrastructure.Data.Entities;

/// <summary>
/// Audit log for security events
/// Tracks all authentication-related events for security monitoring
/// </summary>
[Table("auth_audit_log")]
public class AuthAuditLogEntity
{
    /// <summary>
    /// Primary key - UUID Version 7 for time-ordered, globally unique identification
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Event type: 'login', 'logout', 'token_issued', 'token_revoked', 'failed_auth',
    /// 'permission_denied', 'role_assigned', 'role_removed', 'subject_created', 'subject_deleted'
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the subject involved in this event (if applicable)
    /// </summary>
    [Column("subject_id")]
    public Guid? SubjectId { get; set; }

    /// <summary>
    /// Navigation property to the subject
    /// </summary>
    public SubjectEntity? Subject { get; set; }

    /// <summary>
    /// Foreign key to the refresh token involved in this event (if applicable)
    /// </summary>
    [Column("refresh_token_id")]
    public Guid? RefreshTokenId { get; set; }

    /// <summary>
    /// Navigation property to the refresh token
    /// </summary>
    public RefreshTokenEntity? RefreshToken { get; set; }

    /// <summary>
    /// IP address of the client (supports IPv4 and IPv6)
    /// </summary>
    [MaxLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string from the client
    /// </summary>
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional event details (JSON)
    /// Example: {"permission": "api:entries:read", "resource": "/api/v1/entries"}
    /// </summary>
    [Column("details", TypeName = "jsonb")]
    public string? DetailsJson { get; set; }

    /// <summary>
    /// Whether this event was successful
    /// </summary>
    [Column("success")]
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if the event was not successful
    /// </summary>
    [MaxLength(500)]
    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Correlation ID for tracing related events
    /// </summary>
    [MaxLength(50)]
    [Column("correlation_id")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// When this event occurred
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Constants for auth audit event types
/// </summary>
public static class AuthAuditEventType
{
    public const string Login = "login";
    public const string Logout = "logout";
    public const string TokenIssued = "token_issued";
    public const string TokenRevoked = "token_revoked";
    public const string TokenRefreshed = "token_refreshed";
    public const string FailedAuth = "failed_auth";
    public const string PermissionDenied = "permission_denied";
    public const string RoleAssigned = "role_assigned";
    public const string RoleRemoved = "role_removed";
    public const string SubjectCreated = "subject_created";
    public const string SubjectUpdated = "subject_updated";
    public const string SubjectDeleted = "subject_deleted";
    public const string ApiSecretUsed = "api_secret_used";
    public const string SessionExpired = "session_expired";
}
