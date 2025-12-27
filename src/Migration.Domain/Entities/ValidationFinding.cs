using System;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Domain.Entities;

public class ValidationFinding
{
    public Guid Id { get; set; }
    public Guid ValidationRunId { get; set; }

    public FindingSeverity Severity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? SourcePath { get; set; }
    public string? TargetPath { get; set; }

    public string DetailsJson { get; set; } = "{}"; // JSONB - additional context

    public string? RecommendedAction { get; set; }
    public bool IsResolved { get; set; } = false;

    public DateTime DetectedAt { get; set; }

    // Navigation
    public ValidationRun ValidationRun { get; set; } = null!;
}
