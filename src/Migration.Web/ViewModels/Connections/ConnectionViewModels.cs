using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Web.ViewModels.Connections;

public class ConnectionsIndexViewModel
{
    public IEnumerable<ConnectionListItemViewModel> Connections { get; set; } = Enumerable.Empty<ConnectionListItemViewModel>();
    public ConnectionRole? FilterRole { get; set; }
    public ConnectionType? FilterType { get; set; }
    public ConnectionStatus? FilterStatus { get; set; }
    public string? SearchTerm { get; set; }
}

public class ConnectionListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ConnectionRole Role { get; set; }
    public ConnectionType Type { get; set; }
    public ConnectionStatus Status { get; set; }
    public string EndpointUrl { get; set; } = string.Empty;
    public DateTime? LastVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class ConnectionFormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ConnectionRole Role { get; set; }
    public ConnectionType Type { get; set; }
    public ConnectionStatus Status { get; set; }
    public string EndpointUrl { get; set; } = string.Empty;
    public AuthenticationMode AuthenticationMode { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public ThrottlingProfile ThrottlingProfile { get; set; }
    public bool PreserveAuthorship { get; set; }
    public bool PreserveTimestamps { get; set; }
    public bool ReplaceIllegalCharacters { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public VerificationResult? LastVerificationResult { get; set; }
    public string? LastVerificationDiagnostics { get; set; }
    public int CurrentStep { get; set; }
    public bool IsEditMode { get; set; }
}
