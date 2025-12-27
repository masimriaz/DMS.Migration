namespace DMS.Migration.Domain.Enums;

public enum ConnectionRole
{
    Source = 1,
    Target = 2
}

public enum ConnectionType
{
    SharePointOnPrem = 1,
    SharePointOnline = 2,
    OneDriveForBusiness = 3,
    FileShare = 4
}

public enum ConnectionStatus
{
    Draft = 0,
    Verified = 1,
    Failed = 2,
    Disabled = 3,
    Active = 4,
    Error = 5
}

public enum VerificationStatus
{
    NotStarted = 0,
    Running = 1,
    Success = 2,
    Failed = 3
}

public enum VerificationResult
{
    Success = 1,
    Failed = 2,
    Warning = 3
}

public enum AuthenticationMode
{
    ServiceAccount = 1,
    AppOnly = 2,
    OAuth = 3,
    Anonymous = 4
}

public enum ThrottlingProfile
{
    Normal = 1,
    Insane = 2
}
