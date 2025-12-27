using System.ComponentModel.DataAnnotations;
using DMS.Migration.Domain.Enums;

namespace DMS.Migration.Web.Models;

public class ConnectionViewModel
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    [Required(ErrorMessage = "Connection name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Role is required")]
    public ConnectionRole Role { get; set; }

    [Required(ErrorMessage = "Type is required")]
    public ConnectionType Type { get; set; }

    public ConnectionStatus Status { get; set; }

    [Required(ErrorMessage = "Endpoint URL is required")]
    [StringLength(500)]
    [Url(ErrorMessage = "Please enter a valid URL")]
    public string EndpointUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Authentication mode is required")]
    public string AuthenticationMode { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Username { get; set; }

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    public ThrottlingProfile ThrottlingProfile { get; set; } = ThrottlingProfile.Normal;
    public bool PreserveAuthorship { get; set; }
    public bool PreserveTimestamps { get; set; } = true;
    public bool ReplaceIllegalCharacters { get; set; } = true;

    public DateTime? LastVerifiedAt { get; set; }
    public string? LastVerificationResult { get; set; }
    public string? LastVerificationDiagnostics { get; set; }

    // Wizard state
    public int CurrentStep { get; set; } = 1;
    public bool IsEditMode { get; set; }
}

public class ConnectionsIndexViewModel
{
    public IEnumerable<ConnectionListItemViewModel> Connections { get; set; } = new List<ConnectionListItemViewModel>();
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

public class VerifyConnectionViewModel
{
    public int ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public ConnectionType Type { get; set; }
    public string EndpointUrl { get; set; } = string.Empty;
    public VerificationStatus Status { get; set; }
    public string? Diagnostics { get; set; }
    public string? ErrorMessage { get; set; }
}
