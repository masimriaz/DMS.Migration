namespace DMS.Migration.Domain.Entities;

public class ConnectionSecret
{
    public int Id { get; set; }
    public int ConnectionId { get; set; }

    // Encrypted secret data (password, client secret, etc.)
    public string EncryptedSecret { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Connection Connection { get; set; } = null!;
}
