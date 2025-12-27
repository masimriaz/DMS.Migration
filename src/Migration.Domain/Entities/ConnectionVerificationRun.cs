using System;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

public class ConnectionVerificationRun
{
    public int Id { get; set; }
    public int ConnectionId { get; set; }
    public int TenantId { get; set; }

    public VerificationStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? Result { get; set; }
    public string? Diagnostics { get; set; }
    public string? DiagnosticsJson { get; set; } // JSONB for structured diagnostics
    public string? ErrorMessage { get; set; }

    public string InitiatedBy { get; set; } = string.Empty;

    // Navigation
    public Connection Connection { get; set; } = null!;
}
