using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

public class Connection
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ConnectionRole Role { get; set; }
    public ConnectionType Type { get; set; }
    public ConnectionStatus Status { get; set; }

    // Endpoint
    public string EndpointUrl { get; set; } = string.Empty;

    // Authentication (type-specific fields stored as JSON)
    public string AuthenticationMode { get; set; } = string.Empty; // ServiceAccount, AppOnly, OAuth, Anonymous
    public string? Username { get; set; }

    // Options
    public ThrottlingProfile ThrottlingProfile { get; set; }
    public bool PreserveAuthorship { get; set; }
    public bool PreserveTimestamps { get; set; }
    public bool ReplaceIllegalCharacters { get; set; }

    // Verification
    public DateTime? LastVerifiedAt { get; set; }
    public string? LastVerificationResult { get; set; }
    public string? LastVerificationDiagnostics { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ConnectionSecret? Secret { get; set; }
    public ICollection<ConnectionVerificationRun> VerificationRuns { get; set; } = new List<ConnectionVerificationRun>();
}
