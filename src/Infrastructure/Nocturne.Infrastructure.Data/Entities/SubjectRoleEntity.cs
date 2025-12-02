using System.ComponentModel.DataAnnotations.Schema;

namespace Nocturne.Infrastructure.Data.Entities;

/// <summary>
/// Subject-Role mapping (many-to-many relationship)
/// Links subjects to their assigned roles
/// </summary>
[Table("subject_roles")]
public class SubjectRoleEntity
{
    /// <summary>
    /// Foreign key to the subject
    /// </summary>
    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    /// <summary>
    /// Navigation property to the subject
    /// </summary>
    public SubjectEntity? Subject { get; set; }

    /// <summary>
    /// Foreign key to the role
    /// </summary>
    [Column("role_id")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// Navigation property to the role
    /// </summary>
    public RoleEntity? Role { get; set; }

    /// <summary>
    /// When this role was assigned to the subject
    /// </summary>
    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who assigned this role (null for system-assigned)
    /// </summary>
    [Column("assigned_by_id")]
    public Guid? AssignedById { get; set; }

    /// <summary>
    /// Navigation property to the subject who assigned this role
    /// </summary>
    public SubjectEntity? AssignedBy { get; set; }
}
