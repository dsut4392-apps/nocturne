using Nocturne.Core.Models.Authorization;

namespace Nocturne.Core.Contracts;

/// <summary>
/// Service for recording authentication audit events
/// </summary>
public interface IAuthAuditService
{
    /// <summary>
    /// Record a login event
    /// </summary>
    Task RecordLoginAsync(AuthAuditEvent auditEvent);

    /// <summary>
    /// Record a logout event
    /// </summary>
    Task RecordLogoutAsync(Guid? subjectId, Guid? tokenId, string? ipAddress, string? userAgent);

    /// <summary>
    /// Record a token issued event
    /// </summary>
    Task RecordTokenIssuedAsync(Guid subjectId, Guid tokenId, string tokenType, string? ipAddress, string? userAgent);

    /// <summary>
    /// Record a token revoked event
    /// </summary>
    Task RecordTokenRevokedAsync(Guid? subjectId, Guid tokenId, string reason, string? ipAddress);

    /// <summary>
    /// Record a failed authentication attempt
    /// </summary>
    Task RecordFailedAuthAsync(string reason, string? attemptedIdentity, string? ipAddress, string? userAgent);

    /// <summary>
    /// Record a permission denied event
    /// </summary>
    Task RecordPermissionDeniedAsync(Guid? subjectId, string permission, string resource, string? ipAddress);

    /// <summary>
    /// Get audit events for a subject
    /// </summary>
    /// <param name="subjectId">Subject identifier</param>
    /// <param name="limit">Maximum number of events to return</param>
    /// <param name="offset">Number of events to skip</param>
    /// <returns>List of audit events</returns>
    Task<List<AuthAuditEvent>> GetEventsForSubjectAsync(Guid subjectId, int limit = 100, int offset = 0);

    /// <summary>
    /// Get recent audit events by type
    /// </summary>
    /// <param name="eventType">Event type filter</param>
    /// <param name="limit">Maximum number of events to return</param>
    /// <returns>List of audit events</returns>
    Task<List<AuthAuditEvent>> GetEventsByTypeAsync(string eventType, int limit = 100);

    /// <summary>
    /// Get failed authentication attempts from an IP address
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <param name="since">Only count attempts since this time</param>
    /// <returns>Number of failed attempts</returns>
    Task<int> GetFailedAuthCountAsync(string ipAddress, DateTime since);

    /// <summary>
    /// Delete audit events older than a certain date
    /// </summary>
    /// <param name="olderThan">Delete events before this date</param>
    /// <returns>Number of events deleted</returns>
    Task<int> PruneOldEventsAsync(DateTime olderThan);
}

/// <summary>
/// Authentication audit event
/// </summary>
public class AuthAuditEvent
{
    /// <summary>
    /// Event identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event type
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Subject involved (if any)
    /// </summary>
    public Guid? SubjectId { get; set; }

    /// <summary>
    /// Subject name (for display)
    /// </summary>
    public string? SubjectName { get; set; }

    /// <summary>
    /// Token involved (if any)
    /// </summary>
    public Guid? TokenId { get; set; }

    /// <summary>
    /// Client IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Whether the event was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if not successful
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional details (JSON)
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
